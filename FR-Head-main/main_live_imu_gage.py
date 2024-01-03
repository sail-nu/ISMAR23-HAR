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

from mbientlab.metawear import MetaWear, libmetawear, parse_value
from mbientlab.metawear.cbindings import *
from time import sleep
from threading import Event, Thread
import threading
import os

import platform
import sys

# import resource
from utils.config import get_parser
from utils.visualize import record_skeleton, wrong_analyze
from utils.cls_loss import build_loss

import time
from ctypes import POINTER, WINFUNCTYPE, c_char_p, c_void_p, c_int, c_ulong, c_char_p
from ctypes.wintypes import BOOL, DWORD, BYTE, INT, LPCWSTR, UINT, ULONG

# rlimit = resource.getrlimit(resource.RLIMIT_NOFILE)  # 能打开的最大文件数
# resource.setrlimit(resource.RLIMIT_NOFILE, (2048, rlimit[1]))

addresses = ["F6:A2:51:C8:2B:EC", "F6:DB:70:93:8C:41"]

movement = {addresses[0]: "0", addresses[1]: "1"}

# DECLARE_HANDLE(name) typedef void *name;
HCONV     = c_void_p  # = DECLARE_HANDLE(HCONV)
HDDEDATA  = c_void_p  # = DECLARE_HANDLE(HDDEDATA)
HSZ       = c_void_p  # = DECLARE_HANDLE(HSZ)
LPBYTE    = c_char_p  # POINTER(BYTE)
LPDWORD   = POINTER(DWORD)
LPSTR    = c_char_p
ULONG_PTR = c_ulong

# See windows/ddeml.h for declaration of struct CONVCONTEXT
PCONVCONTEXT = c_void_p

DMLERR_NO_ERROR = 0

# Predefined Clipboard Formats
CF_TEXT         =  1
CF_BITMAP       =  2
CF_METAFILEPICT =  3
CF_SYLK         =  4
CF_DIF          =  5
CF_TIFF         =  6
CF_OEMTEXT      =  7
CF_DIB          =  8
CF_PALETTE      =  9
CF_PENDATA      = 10
CF_RIFF         = 11
CF_WAVE         = 12
CF_UNICODETEXT  = 13
CF_ENHMETAFILE  = 14
CF_HDROP        = 15
CF_LOCALE       = 16
CF_DIBV5        = 17
CF_MAX          = 18

DDE_FACK          = 0x8000
DDE_FBUSY         = 0x4000
DDE_FDEFERUPD     = 0x4000
DDE_FACKREQ       = 0x8000
DDE_FRELEASE      = 0x2000
DDE_FREQUESTED    = 0x1000
DDE_FAPPSTATUS    = 0x00FF
DDE_FNOTPROCESSED = 0x0000

DDE_FACKRESERVED  = (~(DDE_FACK | DDE_FBUSY | DDE_FAPPSTATUS))
DDE_FADVRESERVED  = (~(DDE_FACKREQ | DDE_FDEFERUPD))
DDE_FDATRESERVED  = (~(DDE_FACKREQ | DDE_FRELEASE | DDE_FREQUESTED))
DDE_FPOKRESERVED  = (~(DDE_FRELEASE))

XTYPF_NOBLOCK        = 0x0002
XTYPF_NODATA         = 0x0004
XTYPF_ACKREQ         = 0x0008

XCLASS_MASK          = 0xFC00
XCLASS_BOOL          = 0x1000
XCLASS_DATA          = 0x2000
XCLASS_FLAGS         = 0x4000
XCLASS_NOTIFICATION  = 0x8000

XTYP_ERROR           = (0x0000 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK)
XTYP_ADVDATA         = (0x0010 | XCLASS_FLAGS)
XTYP_ADVREQ          = (0x0020 | XCLASS_DATA | XTYPF_NOBLOCK)
XTYP_ADVSTART        = (0x0030 | XCLASS_BOOL)
XTYP_ADVSTOP         = (0x0040 | XCLASS_NOTIFICATION)
XTYP_EXECUTE         = (0x0050 | XCLASS_FLAGS)
XTYP_CONNECT         = (0x0060 | XCLASS_BOOL | XTYPF_NOBLOCK)
XTYP_CONNECT_CONFIRM = (0x0070 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK)
XTYP_XACT_COMPLETE   = (0x0080 | XCLASS_NOTIFICATION )
XTYP_POKE            = (0x0090 | XCLASS_FLAGS)
XTYP_REGISTER        = (0x00A0 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK )
XTYP_REQUEST         = (0x00B0 | XCLASS_DATA )
XTYP_DISCONNECT      = (0x00C0 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK )
XTYP_UNREGISTER      = (0x00D0 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK )
XTYP_WILDCONNECT     = (0x00E0 | XCLASS_DATA | XTYPF_NOBLOCK)
XTYP_MONITOR         = (0x00F0 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK)

XTYP_MASK            = 0x00F0
XTYP_SHIFT           = 4

TIMEOUT_ASYNC        = 0xFFFFFFFF

def get_winfunc(libname, funcname, restype=None, argtypes=(), _libcache={}):
    """Retrieve a function from a library, and set the data types."""
    from ctypes import windll

    if libname not in _libcache:
        _libcache[libname] = windll.LoadLibrary(libname)
    func = getattr(_libcache[libname], funcname)
    func.argtypes = argtypes
    func.restype = restype

    return func


DDECALLBACK = WINFUNCTYPE(HDDEDATA, UINT, UINT, HCONV, HSZ, HSZ, HDDEDATA, 
                          ULONG_PTR, ULONG_PTR)

class DDE(object):
    """Object containing all the DDE functions"""
    AccessData         = get_winfunc("user32", "DdeAccessData",          LPBYTE,   (HDDEDATA, LPDWORD))
    ClientTransaction  = get_winfunc("user32", "DdeClientTransaction",   HDDEDATA, (LPBYTE, DWORD, HCONV, HSZ, UINT, UINT, DWORD, LPDWORD))
    Connect            = get_winfunc("user32", "DdeConnect",             HCONV,    (DWORD, HSZ, HSZ, PCONVCONTEXT))
    CreateStringHandle = get_winfunc("user32", "DdeCreateStringHandleW", HSZ,      (DWORD, LPCWSTR, UINT))
    Disconnect         = get_winfunc("user32", "DdeDisconnect",          BOOL,     (HCONV,))
    GetLastError       = get_winfunc("user32", "DdeGetLastError",        UINT,     (DWORD,))
    Initialize         = get_winfunc("user32", "DdeInitializeW",         UINT,     (LPDWORD, DDECALLBACK, DWORD, DWORD))
    FreeDataHandle     = get_winfunc("user32", "DdeFreeDataHandle",      BOOL,     (HDDEDATA,))
    FreeStringHandle   = get_winfunc("user32", "DdeFreeStringHandle",    BOOL,     (DWORD, HSZ))
    QueryString        = get_winfunc("user32", "DdeQueryStringA",        DWORD,    (DWORD, HSZ, LPSTR, DWORD, c_int))
    UnaccessData       = get_winfunc("user32", "DdeUnaccessData",        BOOL,     (HDDEDATA,))
    Uninitialize       = get_winfunc("user32", "DdeUninitialize",        BOOL,     (DWORD,))

class DDEError(RuntimeError):
    """Exception raise when a DDE errpr occures."""
    def __init__(self, msg, idInst=None):
        if idInst is None:
            RuntimeError.__init__(self, msg)
        else:
            RuntimeError.__init__(self, "%s (err=%s)" % (msg, hex(DDE.GetLastError(idInst))))

class DDEClient(object):
    """The DDEClient class.

    Use this class to create and manage a connection to a service/topic.  To get
    classbacks subclass DDEClient and overwrite callback."""

    def __init__(self, service, topic):
        """Create a connection to a service/topic."""
        from ctypes import byref

        self._idInst = DWORD(0)
        self._hConv = HCONV()

        self._callback = DDECALLBACK(self._callback)
        res = DDE.Initialize(byref(self._idInst), self._callback, 0x00000010, 0)
        if res != DMLERR_NO_ERROR:
            raise DDEError("Unable to register with DDEML (err=%s)" % hex(res))

        hszService = DDE.CreateStringHandle(self._idInst, service, 1200)
        hszTopic = DDE.CreateStringHandle(self._idInst, topic, 1200)
        self._hConv = DDE.Connect(self._idInst, hszService, hszTopic, PCONVCONTEXT())
        DDE.FreeStringHandle(self._idInst, hszTopic)
        DDE.FreeStringHandle(self._idInst, hszService)
        if not self._hConv:
            raise DDEError("Unable to establish a conversation with server", self._idInst)

    def __del__(self):
        """Cleanup any active connections."""
        if self._hConv:
            DDE.Disconnect(self._hConv)
        if self._idInst:
            DDE.Uninitialize(self._idInst)

    def advise(self, item, stop=False):
        """Request updates when DDE data changes."""
        from ctypes import byref

        hszItem = DDE.CreateStringHandle(self._idInst, item, 1200)
        hDdeData = DDE.ClientTransaction(LPBYTE(), 0, self._hConv, hszItem, CF_TEXT, XTYP_ADVSTOP if stop else XTYP_ADVSTART, TIMEOUT_ASYNC, LPDWORD())
        DDE.FreeStringHandle(self._idInst, hszItem)
        if not hDdeData:
            raise DDEError("Unable to %s advise" % ("stop" if stop else "start"), self._idInst)
            DDE.FreeDataHandle(hDdeData)

    def execute(self, command, timeout=5000):
        """Execute a DDE command."""
        pData = c_char_p(command)
        cbData = DWORD(len(command) + 1)
        hDdeData = DDE.ClientTransaction(pData, cbData, self._hConv, HSZ(), CF_TEXT, XTYP_EXECUTE, timeout, LPDWORD())
        if not hDdeData:
            raise DDEError("Unable to send command", self._idInst)
            DDE.FreeDataHandle(hDdeData)

    def request(self, item, timeout=5000):
        """Request data from DDE service."""
        from ctypes import byref

        hszItem = DDE.CreateStringHandle(self._idInst, item, 1200)
        hDdeData = DDE.ClientTransaction(LPBYTE(), 0, self._hConv, hszItem, CF_TEXT, XTYP_REQUEST, timeout, LPDWORD())
        DDE.FreeStringHandle(self._idInst, hszItem)
        if not hDdeData:
            raise DDEError("Unable to request item", self._idInst)

        if timeout != TIMEOUT_ASYNC:
            pdwSize = DWORD(0)
            pData = DDE.AccessData(hDdeData, byref(pdwSize))
        #if not pData:
        #    DDE.FreeDataHandle(hDdeData)
        #    raise DDEError("Unable to access data", self._idInst)
        #    # TODO: use pdwSize
        #    DDE.UnaccessData(hDdeData)
        #else:
        #    pData = None
        #    DDE.FreeDataHandle(hDdeData)
        return pData

    def callback(self, value, item=None):
        """Calback function for advice."""
        # print "%s: %s" % (item, value)

    def _callback(self, wType, uFmt, hConv, hsz1, hsz2, hDdeData, dwData1, dwData2):
        #if wType == XTYP_ADVDATA:
        from ctypes import byref, create_string_buffer

        dwSize = DWORD(0)
        pData = DDE.AccessData(hDdeData, byref(dwSize))
        if pData:
            item = create_string_buffer('\000' * 128)
            DDE.QueryString(self._idInst, hsz2, item, 128, 1004)
            self.callback(pData, item.value)
            DDE.UnaccessData(hDdeData)
        return DDE_FACK
        return 0

def WinMSGLoop():
    """Run the main windows message loop."""
    from ctypes import POINTER, byref, c_ulong
    from ctypes.wintypes import BOOL, HWND, MSG, UINT

    LPMSG = POINTER(MSG)
    LRESULT = c_ulong
    GetMessage = get_winfunc("user32", "GetMessageW", BOOL, (LPMSG, HWND, UINT, UINT))
    TranslateMessage = get_winfunc("user32", "TranslateMessage", BOOL, (LPMSG,))
    # restype = LRESULT
    DispatchMessage = get_winfunc("user32", "DispatchMessageW", LRESULT, (LPMSG,))

    msg = MSG()
    lpmsg = byref(msg)
    while GetMessage(lpmsg, HWND(), 0, 0) > 0:
        TranslateMessage(lpmsg)
        DispatchMessage(lpmsg)

class State:
    # init
    def __init__(self, device):
        self.device = device
        self.samples = 0
        self.movement = False
        self.prev_value = None
        self.callback = FnVoid_VoidP_DataP(self.data_handler)

    # callback
    def data_handler(self, ctx, data):
        global movement
        value = parse_value(data)
        if self.prev_value == None:
            self.prev_value = value
        # print("ACC X: %s -> %s" % (self.device.address, value))
        if abs(value - self.prev_value) > 0.01:
            movement[self.device.address] = "1"
            print(self.device.address+": movement!!!!") 
        else:
            movement[self.device.address] = "0"
        self.prev_value = value


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
        self.step_completion = "000000000"
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
    
    def imu_stream(self):
        states = []
        # connect
        for address in addresses:
            d = MetaWear(address)
            d.connect()
            print("Connected to " + d.address + " over " + ("USB" if d.usb.is_connected else "BLE"))
            states.append(State(d))

        # configure
        for s in states:
            print("Configuring device")
            libmetawear.mbl_mw_settings_set_connection_parameters(s.device.board, 7.5, 7.5, 0, 6000)
            sleep(1.5)
            # generic acc calls - configure
            libmetawear.mbl_mw_acc_set_odr(s.device.board, 10.0)
            libmetawear.mbl_mw_acc_set_range(s.device.board, 16.0)
            libmetawear.mbl_mw_acc_write_acceleration_config(s.device.board)
            # get acc - x signal
            signal_acc = libmetawear.mbl_mw_acc_get_acceleration_data_signal(s.device.board)
            signal_acc_x = libmetawear.mbl_mw_datasignal_get_component(signal_acc, Const.ACC_ACCEL_X_AXIS_INDEX)
            libmetawear.mbl_mw_datasignal_subscribe(signal_acc_x, None, s.callback)
            # start acc
            libmetawear.mbl_mw_acc_enable_acceleration_sampling(s.device.board)
            libmetawear.mbl_mw_acc_start(s.device.board)
    
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


    def track_step(self, action, measurement, part_move, gage_move, record_steps):

        print(">>>>>record steps: ", record_steps)

        self.step_completion = list(self.step_completion)

        # step 0 - Locate Caliper
        if gage_move == "1":
            self.step_completion[0] = "1"
        
        # step 1 - Turn on
        if measurement != "":
            self.step_completion[1] = "1"
        
        # step 2 - Inch mode
        if measurement[-2:] == "in":
            self.step_completion[2] = "1"

        # step 3 - Wipe clean
        if action == "Clean":
            self.step_completion[3] = "1"

        # step 4 - Zero
        if action == "Press" and measurement[:6] == "0.0000":
            if self.step_completion[2] == "1" and self.step_completion[3] == "1":
                self.step_completion[4] = "1"
            else:
                if self.step_completion[2] == "0":
                    self.step_completion[2] = "2"
                elif self.step_completion[3] == "0":
                    self.step_completion[3] = "2"

        # Step 5 - Measure x 1
        if record_steps[0] == "1":
            if self.step_completion[4] == "1":
                self.step_completion[5] = "1"
            else:
                if self.step_completion[0] == "0":
                    self.step_completion[0] = "2"
                elif self.step_completion[1] == "0":
                    self.step_completion[1] = "2"
                elif self.step_completion[2] == "0":
                    self.step_completion[2] = "2"
                elif self.step_completion[3] == "0":
                    self.step_completion[3] = "2"
                elif self.step_completion[4] == "0":
                    self.step_completion[4] = "2"

        # Step 6 - Measure x 2
        if record_steps[1] == "1":
            self.step_completion[6] = "1"

        # Step 7 - Measure x 3
        if record_steps[2] == "1":
            self.step_completion[7] = "1"

        # Step 8 - Final check
        if record_steps[3] == "1":
            self.step_completion[8] = "1"
        


        self.step_completion = "".join(self.step_completion)
   
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
        # get connect with server SPnet
        dde = DDEClient("DSA", "F8:5A:9A:56:3A:B4")
        self.imu_stream()
        while True:
            try:
                #gage measurement
                gage_data = str(dde.request("Data"))[7:-1]
                print('Gage measurement: ', gage_data)
                # Receive data from the Unity app
                data = client_socket.recv(1024)
                received_data = data.decode()
                # print('Received data:', received_data)
                record_steps = str(received_data[-4:])

                # Process the received data (example: convert the tuple values to a string)
                received_tuple = eval(received_data[:-4])  # Safely evaluate the received string as a tuple
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
                    if movement[addresses[0]] + movement[addresses[1]] == "00":
                        result = 'No Action'

                    self.track_step(result, gage_data, movement[addresses[0]], movement[addresses[1]], record_steps)

                    result = result + gage_data + self.step_completion + movement[addresses[0]] + movement[addresses[1]]
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
