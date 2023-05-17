﻿using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace Dissonance.Integrations.PhotonUnityNetworking2
{
    public class PhotonPlayer
        : MonoBehaviourPunCallbacks, IDissonancePlayer, IPunInstantiateMagicCallback
    {
        private static readonly Log Log = Logs.Create(LogCategory.Network, "Photon Player Component");

        private DissonanceComms _comms;

        private Coroutine _startCo;

        public bool IsTracking { get; private set; }

        public string PlayerId { get; private set; }

        // Cache `transform`
        private Transform _transform;
        private Transform Transform
        {
            get
            {
                if (_transform == null)
                    _transform = transform;
                return _transform;
            }
        }

        public Vector3 Position
        {
            get { return Transform.position; }
        }

        public Quaternion Rotation
        {
            get { return Transform.rotation; }
        }

        public NetworkPlayerType Type
        {
            get
            {
                if (photonView == null)
                    return NetworkPlayerType.Unknown;
                return photonView.IsMine ? NetworkPlayerType.Local : NetworkPlayerType.Remote;
            }
        }

        void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
        {
            _comms = FindObjectOfType<DissonanceComms>();
            if (_comms == null)
            {
                throw Log.CreateUserErrorException(
                    "cannot find DissonanceComms component in scene",
                    "not placing a DissonanceComms component on a game object in the scene",
                    "https://placeholder-software.co.uk/dissonance/docs/Basics/Quick-Start-Photon.html",
                    "00077AC8-3CBF-4DD8-A1C7-3ED3E8F64914");
            }

            if (info.Sender.IsLocal)
            {
                // This method is called on the client which has control authority over this object. This will be the local client of whichever player we are tracking.
                if (_comms.LocalPlayerName != null)
                    SetPlayerName(_comms.LocalPlayerName);

                //Subscribe to future name changes (this is critical because we may not have run the initial set name yet and this will trigger that initial call)
                _comms.LocalPlayerNameChanged += SetPlayerName;
            }
        }

        public void OnDestroy()
        {
            if (_comms != null)
                _comms.LocalPlayerNameChanged -= SetPlayerName;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (!IsTracking)
                StartTracking();
        }

        public override void OnDisable()
        {
            if (IsTracking)
                StopTracking();

            base.OnDisable();
        }

        [PunRPC]
        private void SetPlayerName(string playerName)
        {
            //We need to stop and restart tracking to handle the name change
            if (IsTracking)
                StopTracking();

            //Perform the actual work
            PlayerId = playerName;
            StartTracking();

            //Inform the other clients the name has changed, if we are the owner
            if (photonView.IsMine)
                photonView.RPC("SetPlayerName", RpcTarget.OthersBuffered, PlayerId);
        }

        private void StartTracking()
        {
            if (IsTracking)
                throw Log.CreatePossibleBugException("Attempting to start player tracking, but tracking is already started", "0663D808-ACCC-4D13-8913-03F9BA0C8578");

            _startCo = StartCoroutine(StartTrackingCo());
        }

        private IEnumerator StartTrackingCo()
        {
            // Wait until Dissonance comms object is initialised
            while (_comms == null)
            {
                _comms = FindObjectOfType<DissonanceComms>();
                yield return null;
            }

            // Can't track someone with a null name! Loop until name is valid
            while (PlayerId == null)
                yield return null;

            // Now start tracking
            _comms.TrackPlayerPosition(this);
            IsTracking = true;

            _startCo = null;
        }

        private void StopTracking()
        {
            if (!IsTracking)
                throw Log.CreatePossibleBugException("Attempting to stop player tracking, but tracking is not started", "48802E32-C840-4C4B-BC58-4DC741464B9A");

            // Stop startup coroutine if it is running
            if (_startCo != null)
                StopCoroutine(_startCo);
            _startCo = null;

            if (_comms != null)
            {
                _comms.StopTracking(this);
                IsTracking = false;
            }
        }
    }
}
