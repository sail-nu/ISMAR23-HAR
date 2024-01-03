#!/usr/bin/env python
from __future__ import print_function
import socket
import inspect
import os
import pickle
import random
import shutil
import sys
import time
from collections import OrderedDict
import traceback
from sklearn.metrics import confusion_matrix
import csv
import numpy as np
import glob
import itertools
import math
# torch
import torch
import torch.backends.cudnn as cudnn
import torch.nn as nn
import torch.optim as optim
import yaml
from tensorboardX import SummaryWriter
from tqdm import tqdm
from torchsummary import summary

# import resource
from utils.config import get_parser
from utils.visualize import record_skeleton, wrong_analyze
from utils.cls_loss import build_loss

# rlimit = resource.getrlimit(resource.RLIMIT_NOFILE)  # 能打开的最大文件数
# resource.setrlimit(resource.RLIMIT_NOFILE, (2048, rlimit[1]))


def init_seed(seed):
    # 随机种子固定
    torch.cuda.manual_seed_all(seed)
    torch.manual_seed(seed)
    np.random.seed(seed)
    random.seed(seed)
    # torch.backends.cudnn.enabled = False
    torch.backends.cudnn.deterministic = True
    torch.backends.cudnn.benchmark = False


def import_class(import_str):
    # 动态 import
    mod_str, _sep, class_str = import_str.rpartition('.')  # 从目标字符串的末尾也就是右边开始搜索分割符
    # print('import->',mod_str, class_str)
    __import__(mod_str)
    try:
        return getattr(sys.modules[mod_str], class_str)
    except AttributeError:
        raise ImportError('Class %s cannot be found (%s)' % (class_str, traceback.format_exception(*sys.exc_info())))

def rand_view_transform(X, agx, agy, s):
        agx = math.radians(agx)
        agy = math.radians(agy)
        Rx = np.asarray([[1,0,0], [0,math.cos(agx),math.sin(agx)], [0, -math.sin(agx),math.cos(agx)]])
        Ry = np.asarray([[math.cos(agy), 0, -math.sin(agy)], [0,1,0], [math.sin(agy), 0, math.cos(agy)]])
        Ss = np.asarray([[s,0,0],[0,s,0],[0,0,s]])
        # print('X Shape = ',X.shape)
        X0 = np.dot(np.reshape(X,(-1,3)), np.dot(Ry,np.dot(Rx,Ss)))
        X = np.reshape(X0, X.shape)
        return X

def preprocess(value):
        random.random()
        agx = 0
        agy = 0
        s = 1.0
        center = value[0,1,:]
        value = value - center
        scalerValue = rand_view_transform(value, agx, agy, s)
        scalerValue = np.reshape(scalerValue, (-1, 3))
        scalerValue = (scalerValue - np.min(scalerValue,axis=0)) / (np.max(scalerValue,axis=0) - np.min(scalerValue,axis=0))
        scalerValue = scalerValue*2-1

        scalerValue = np.reshape(scalerValue, (-1, 26, 3))

        data = np.zeros( (52, 26, 3) )

        value = scalerValue[:,:,:]
        length = value.shape[0]

        idx = np.linspace(0,length-1,52).astype(np.int)
        data[:,:,:] = value[idx,:,:] # T,V,C

        data = np.transpose(data, (2, 0, 1))
        C,T,V = data.shape
        data = np.reshape(data,(1, C,T,V,1))

        return data


class Processor:
    """ 
        Processor for Skeleton-based Action Recgnition
    """

    def __init__(self, arg):
        self.arg = arg
        # self.save_arg()
        # if arg.phase == 'train':  # train = train + val
        #     if not arg.train_feeder_args['debug']:
        #         arg.model_saved_name = os.path.join(arg.work_dir, 'runs')  # work_dir/runs 模型记录
        #         if os.path.isdir(arg.model_saved_name):
        #             print('log_dir: ', arg.model_saved_name, 'already exist')
        #             answer = input('delete it? y/n:')
        #             if answer == 'y':
        #                 shutil.rmtree(arg.model_saved_name)
        #                 print('Dir removed: ', arg.model_saved_name)
        #                 input('Refresh the website of tensorboard by pressing any keys')
        #             else:
        #                 print('Dir not removed: ', arg.model_saved_name)
        #         self.train_writer = SummaryWriter(os.path.join(arg.model_saved_name, 'train'), 'train')
        #         self.val_writer = SummaryWriter(os.path.join(arg.model_saved_name, 'val'), 'val')
        #     else:
        #         self.train_writer = self.val_writer = SummaryWriter(os.path.join(arg.model_saved_name, 'test'), 'test')
        # self.global_step = 0
        # pdb.set_trace()
        self.load_model()
        self.model = self.model.cuda(self.output_device)

        if type(self.arg.device) is list:
            if len(self.arg.device) > 1:
                self.model = nn.DataParallel(
                    self.model,
                    device_ids=self.arg.device,
                    output_device=self.output_device)

    
    def load_model(self):
        output_device = self.arg.device[0] if type(self.arg.device) is list else self.arg.device
        self.output_device = output_device
        Model = import_class(self.arg.model)
        shutil.copy2(inspect.getfile(Model), self.arg.work_dir)
        # print(Model)
        self.model = Model(**self.arg.model_args, cl_mode=self.arg.cl_mode,
                           multi_cl_weights=self.arg.w_multi_cl_loss, cl_version=self.arg.cl_version,
                           pred_threshold=self.arg.pred_threshold, use_p_map=self.arg.use_p_map)
        # print(summary(self.model))
        self.loss = build_loss(self.arg).cuda(output_device)

        if self.arg.weights:
            self.global_step = 0
            try:
                self.global_step = int(arg.weights[:-3].split('-')[-1])
            except:
                pass

            # self.print_log('Load weights from {}.'.format(self.arg.weights))
            if '.pkl' in self.arg.weights:
                with open(self.arg.weights, 'r') as f:
                    weights = pickle.load(f)
            else:
                weights = torch.load(self.arg.weights)

            weights = OrderedDict([[k.split('module.')[-1], v.cuda(output_device)] for k, v in weights.items()])

            keys = list(weights.keys())
            # for w in self.arg.ignore_weights:
            #     for key in keys:
            #         if w in key:
            #             if weights.pop(key, None) is not None:
            #                 self.print_log('Sucessfully Remove Weights: {}.'.format(key))
            #             else:
            #                 self.print_log('Can Not Remove Weights: {}.'.format(key))
            
            try:
                print('Weight loaded successfully: ', self.arg.weights)
                self.model.load_state_dict(weights)
            except:
                state = self.model.state_dict()
                diff = list(set(state.keys()).difference(set(weights.keys())))
                print('Can not find these weights:')
                for d in diff:
                    print('  ' + d)
                state.update(weights)
                self.model.load_state_dict(state, strict=False)


 
   
    def start(self):
        self.model.eval()
        labels = ['Hold', 'Slide by Holding', 'Put Down', 'Pick Up', 'Roll with Thumb', 'Rotate', 'Screw', 'Lock', 'Press', 'Shake', 'Open', 'Close', 'Clean', 'Flip', 'Attach', 'Detach']
        input_data = np.zeros((52, 26, 3))
        # data_dict = pickle.load(open("data/PHI/test_set.pickle", "rb"))
        # for i in range(200):
        #     val = np.random.rand(100, 26, 3)
        #     input = preprocess(np.transpose(val, (2, 0, 1)))
        #     input = torch.from_numpy(input).float().cuda(self.output_device)
        #     output = self.model(torch.from_numpy(np.random.rand(1, 3, 52, 26, 1)).float().cuda(self.output_device))
        #     _, predict_label = torch.max(output.data, 1)
        #     predict_label = labels[predict_label[0]]
        #     print(predict_label)
        while True:
            try:
                # Receive data from the Unity app
                data = client_socket.recv(1024)
                received_data = data.decode()
                # print('Received data:', received_data)

                # Process the received data (example: convert the tuple values to a string)
                received_tuple = eval(received_data)  # Safely evaluate the received string as a tuple
                # Convert the tuple to a NumPy array
                data_arr = np.array(received_tuple)
                # Reshape the array into a 26x3 array
                data_arr = data_arr.reshape((26, 3))

                # print('Received data:', data_arr)

                input_data[:-1] = input_data[1:]
                input_data[-1] = data_arr

                # input_data[1:] = input_data[:-1]
                # input_data[0] = data_arr
                with torch.no_grad():
                    input = preprocess(input_data)
                    # input = preprocess(np.transpose(input_data, (2, 0, 1)))
                    # print('transpose = ',np.transpose(input_data, (2, 0, 1)).shape)
                    input = torch.from_numpy(input).float().cuda(self.output_device)
                    # output = self.model(torch.from_numpy(np.random.rand(1, 3, 52, 26, 1)).float().cuda(self.output_device))
                    output = self.model(input)
                    _, predict_label = torch.max(output.data, 1)
                    predict_label = labels[predict_label[0]]
                    print(predict_label)
                    # print(torch.topk(output.data, 16)[1])
                    print("Predicted Actions: ")
                    # print(input)
                    # text = ""
                    # for action in torch.topk(output.data, 5)[1][0]:
                    #     text+=labels[action]+'\n'

                    if input_data[-1, 0, 0] == 0:
                        result = 'No Action'
                    else:
                        result = predict_label
                        # result = predict_label
                    # print('Processed result:', result)

                    # Send the processed result back to the Unity app
                    client_socket.sendall(result.encode())
                    # print('Result sent back to the Unity app.')
                    # print(result)

            except Exception as e:
                print('An error occurred:', str(e))
                break

        # data = np.zeros((3, 52, 26))
        # data = preprocess(data)
        # data = torch.from_numpy(data).float().cuda(self.output_device)
        # output = self.model(data)
        # print(output.shape)

# if __name__ == '__main__':
#     # os.environ['CUDA_DEVICE_ORDER'] = 'PCI_BUS_ID'


parser = get_parser()

# load arg from config file
p = parser.parse_args()
if p.config is not None:
    with open(p.config, 'r') as f:
        default_arg = yaml.load(f)
    key = vars(p).keys()
    for k in default_arg.keys():
        if k not in key:
            print('WRONG ARG: {}'.format(k))
            assert (k in key)
    parser.set_defaults(**default_arg)

arg = parser.parse_args()
init_seed(arg.seed)
processor = Processor(arg)

# Create a TCP/IP socket
server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# Bind the socket to a specific address and port
server_address = ('localhost', 8888)  # Replace with your desired server address and port
server_socket.bind(server_address)

# Listen for incoming connections
server_socket.listen(1)

print('Python server is up and running. Waiting for a connection...')

# Wait for a connection
client_socket, client_address = server_socket.accept()
print('Connected to:', client_address)

try:
    processor.start()
except:
    processor.print_log(str(traceback.format_exc()), False)

# Close the connection
client_socket.close()
print('Connection closed.')
