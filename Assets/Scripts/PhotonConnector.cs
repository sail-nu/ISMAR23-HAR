using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonConnector : MonoBehaviourPunCallbacks
{
    public string roomName = "TestRoom";
    public GameObject ObjectToSpawn;

    private State _state;
    private enum State
    {
        Connecting,
        ServerList,
        JoiningRoom,
        CreatingRoom,
        InRoom
    }

    private void Start()
    {
        StartCoroutine(StartConnection());    
    }

    private IEnumerator StartConnection()
    {
        PhotonNetwork.NetworkStatisticsEnabled = true;
        PhotonNetwork.ConnectUsingSettings();

        _state = State.Connecting;

        //Wait until connected
        while (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer)
            yield return true;

        PhotonNetwork.JoinLobby();

        _state = State.ServerList;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        base.OnDisable();
    }

    public override void OnJoinedRoom()
    {
        //PhotonNetwork.LoadLevel(SceneName ?? "PUN2 Game World");
        _state = State.InRoom;

        if ( ObjectToSpawn != null ) PhotonNetwork.Instantiate(ObjectToSpawn.name, Vector3.zero, Quaternion.identity, 0);

        //PhotonNetwork.LeaveRoom();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("PhotonNetworkManager:OnJoinedLobby");
        //PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions(), new TypedLobby());

        RoomOptions ro = new RoomOptions();
        ro.CleanupCacheOnLeave = true;
        PhotonNetwork.JoinOrCreateRoom(roomName, ro, TypedLobby.Default);
        _state = State.CreatingRoom;
        _state = State.JoiningRoom;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        Debug.LogError("PhotonConnector:Disconnected because:" + cause.ToString());// + " attempt to reconnect");

        //StartConnection();
        //PhotonNetwork.Disconnect();
        //PhotonNetwork.ReconnectAndRejoin();
        //Debug.LogError("PhotonConnector:Done attempt to reconnect");
    }

    /*
    void ILobbyCallbacks.OnRoomListUpdate(List<RoomInfo> roomList)
    {
        _rooms = roomList;
        Debug.Log("Received Photon Room List");
    }

    void IMatchmakingCallbacks.OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogError(string.Format("Failed to join photon room: '{0}'", message));

        _state = State.ServerList;
    }

    void IMatchmakingCallbacks.OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError(string.Format("Failed to join photon room: '{0}'", message));

        _state = State.ServerList;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    */
}
