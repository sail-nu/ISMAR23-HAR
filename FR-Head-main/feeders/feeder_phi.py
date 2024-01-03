import numpy as np
import pickle
import json
import random
import math

from torch.utils.data import Dataset

phi_class_name = ['Hold', 'Slide_H', 'PDown', 'PUp', 'Roll_T', 'Rotate', 'Screw', 'Lock', 'Press', 'Shake', 'Open', 'Close', 'Clean', 'Flip', 'Attach', 'Detach']


class Feeder(Dataset):
    def __init__(self, data_path, label_path, repeat=1, random_choose=False, random_shift=False, random_move=False,
                 window_size=-1, normalization=False, debug=False, use_mmap=True):

        if 'val' in label_path:
            self.train_val = 'val'
            self.data_dict = pickle.load(open("data/PHI/test_set.pickle", "rb"))
        else:
            self.train_val = 'train'
            self.data_dict = pickle.load(open("data/PHI/train_set.pickle", "rb"))

        # self.nw_ucla_root = 'data/NW-UCLA/all_sqe/'
        self.time_steps = 52
        self.num_d = 3
        self.bone_vectors = [(3, 1), (4, 3), (5, 4), (6, 5), (7, 1), (8, 7), (9, 8), (10, 9), (11, 10), (12, 1), (13, 12), (14, 13), (15, 14), (16, 15), (17, 1), (18, 17), (19, 18), (20, 19), (21, 20), (22, 1), (23, 22), (24, 23), (25, 24), (26, 25)]
        self.label = []
        for index in range(len(self.data_dict['label'])):
            # info = self.data_dict[index]
            self.label.append(int(self.data_dict['label'][index]) - 1)
        # print('hello')
        self.debug = debug
        self.data_path = data_path
        self.label_path = label_path
        self.random_choose = random_choose
        self.random_shift = random_shift
        self.random_move = random_move
        self.window_size = window_size
        self.normalization = normalization
        self.use_mmap = use_mmap
        self.repeat = repeat
    
        self.load_data()
        # print(len(self.data))
        # print(self.data[0].shape)

        if normalization:
            self.get_mean_map()
        self.bone = 'bone' in self.data_path
        self.vel = 'motion' in self.data_path

    def load_data(self):
        # data: N C V T M
        self.data = self.data_dict['pose']
        if 'rot' in self.data_path:
            self.num_d = 7
            self.data = []
            for idx in range(len(self.data_dict['pose'])):
                value = np.concatenate([self.data_dict['pose'][idx], self.data_dict['rot'][idx]], axis=2)
                self.data.append(value)


    def get_mean_map(self):
        data = self.data
        N, C, T, V, M = data.shape
        self.mean_map = data.mean(axis=2, keepdims=True).mean(axis=4, keepdims=True).mean(axis=0)
        self.std_map = data.transpose((0, 2, 4, 1, 3)).reshape((N * T * M, C * V)).std(axis=0).reshape((C, 1, V, 1))

    def __len__(self):
        return len(self.data_dict['pose'])*self.repeat

    def __iter__(self):
        return self

    def rand_view_transform(self,X, agx, agy, s):
        agx = math.radians(agx)
        agy = math.radians(agy)
        Rx = np.asarray([[1,0,0], [0,math.cos(agx),math.sin(agx)], [0, -math.sin(agx),math.cos(agx)]])
        Ry = np.asarray([[math.cos(agy), 0, -math.sin(agy)], [0,1,0], [math.sin(agy), 0, math.cos(agy)]])
        Ss = np.asarray([[s,0,0],[0,s,0],[0,0,s]])
        # print('X Shape = ',X.shape)
        X0 = np.dot(np.reshape(X,(-1,3)), np.dot(Ry,np.dot(Rx,Ss)))
        X = np.reshape(X0, X.shape)
        return X
    
    def rand_view_transform_rot(self, X, agx, agy, s):
        agx = math.radians(agx)
        agy = math.radians(agy)
        Rx = np.array([[1, 0, 0], [0, math.cos(agx), math.sin(agx)], [0, -math.sin(agx), math.cos(agx)]])
        Ry = np.array([[math.cos(agy), 0, -math.sin(agy)], [0, 1, 0], [math.sin(agy), 0, math.cos(agy)]])
        Ss = np.array([[s, 0, 0], [0, s, 0], [0, 0, s]])

        num_joints = X.shape[1]
        positions = X[:, :, :3]  # Extract 3D joint positions
        orientations = X[:, :, 3:]  # Extract quaternion orientations

        # Apply transformations to joint positions
        positions_transformed = np.dot(np.reshape(positions,(-1,3)), np.dot(Ry, np.dot(Rx, Ss)))
        positions = np.reshape(positions_transformed, positions.shape)
        # Combine transformed positions with original orientations
        X_transformed = np.concatenate((positions, orientations), axis=2)

        return X_transformed

    def __getitem__(self, index):
        label = self.label[index % len(self.data_dict['pose'])]
        value = self.data[index % len(self.data_dict['pose'])]
        # print('value shape = ',value.shape)

        if self.train_val == 'train':
            random.random()
            agx = random.randint(-60, 60)
            agy = random.randint(-60, 60)
            s = random.uniform(0.5, 1.5)

            center = value[0,1,:]
            value = value - center
            
            if 'rot' in self.data_path:
                scalerValue = self.rand_view_transform_rot(value, agx, agy, s)
            else:
                scalerValue = self.rand_view_transform(value, agx, agy, s)

            scalerValue = np.reshape(scalerValue, (-1, self.num_d))
            scalerValue = (scalerValue - np.min(scalerValue,axis=0)) / (np.max(scalerValue,axis=0) - np.min(scalerValue,axis=0))
            scalerValue = scalerValue*2-1
            scalerValue = np.reshape(scalerValue, (-1, 26, self.num_d))

            data = np.zeros( (self.time_steps, 26, self.num_d) )

            value = scalerValue[:,:,:]
            length = value.shape[0]

            random_idx = random.sample(list(np.arange(length))*100, self.time_steps)
            random_idx.sort()
            data[:,:,:] = value[random_idx,:,:]
            data[:,:,:] = value[random_idx,:,:]

        else:
            random.random()
            agx = 0
            agy = 0
            s = 1.0

            center = value[0,1,:]
            value = value - center
            if 'rot' in self.data_path:
                scalerValue = self.rand_view_transform_rot(value, agx, agy, s)
            else:
                scalerValue = self.rand_view_transform(value, agx, agy, s)

            scalerValue = np.reshape(scalerValue, (-1, self.num_d))
            scalerValue = (scalerValue - np.min(scalerValue,axis=0)) / (np.max(scalerValue,axis=0) - np.min(scalerValue,axis=0))
            scalerValue = scalerValue*2-1

            scalerValue = np.reshape(scalerValue, (-1, 26, self.num_d))

            data = np.zeros( (self.time_steps, 26, self.num_d) )

            value = scalerValue[:,:,:]
            length = value.shape[0]

            idx = np.linspace(0,length-1,self.time_steps).astype(np.int)
            data[:,:,:] = value[idx,:,:] # T,V,C

        if 'bone' in self.data_path:
            data_bone = np.zeros_like(data)
            for bone_idx in range(26):
                data_bone[:, self.bone_vectors[bone_idx][0] - 1, :] = data[:, self.bone_vectors[bone_idx][0] - 1, :] - data[:, self.bone_vectors[bone_idx][1] - 1, :]
            data = data_bone

        if 'motion' in self.data_path:
            data_motion = np.zeros_like(data)
            data_motion[:-1, :, :] = data[1:, :, :] - data[:-1, :, :]
            data = data_motion
        data = np.transpose(data, (2, 0, 1))
        C,T,V = data.shape
        data = np.reshape(data,(C,T,V,1))
        # print('data shape = ',data.shape)

        return data, label, index

    def top_k(self, score, top_k):
        rank = score.argsort()
        hit_top_k = [l in rank[i, -top_k:] for i, l in enumerate(self.label)]
        return sum(hit_top_k) * 1.0 / len(hit_top_k)


def import_class(name):
    components = name.split('.')
    mod = __import__(components[0])
    for comp in components[1:]:
        mod = getattr(mod, comp)
    return mod