using UnityEngine;
using System.Collections;
using System.Linq;
using Photon.Pun;
#if UNITY_WSA
using UnityEngine.Windows.WebCam;
#endif
using Photon.Realtime;
using TMPro;
using System.Threading;
using System;
using UnityEngine.UI;

public class RemoteAssistHandler : MonoBehaviour
{
    public Renderer photoRenderer;
    public Renderer photoRendererLocal;

    public RawImage photoImage;
    public RawImage photoImageLocal;

    public PhotonView view;
    public TMP_Text debugText;
    public TMP_Text connectionText;

#if UNITY_EDITOR
    bool isHololens = false;
#else
    bool isHololens = true;
#endif

#if UNITY_WSA
    PhotoCapture photoCaptureObject = null;
#endif
    Texture2D targetTexture = null;
    WebCamTexture webCamTexture;

    Resolution cameraResolution;
    byte[] bytes;
    byte[] packetBytesSend;
    byte[] packetBytesReceive;

    // Use this for initialization
    void Start()
    {
        if ( view == null ) view = GetComponent<PhotonView>();
        if (isHololens) return;

        Debug.LogError("Starting web cam");
        webCamTexture = new WebCamTexture();
        webCamTexture.Play();
        if (photoImageLocal != null) photoImageLocal.texture = webCamTexture;
        if (photoRendererLocal != null) photoRendererLocal.material.SetTexture("_MainTex", webCamTexture);


        //#if !UNITY_EDITOR 
        // StartPictureDelayed();
        //#endif
    }

    void PrintMessage(string s)
    {
        Debug.LogError("RemoteAssist:" + s);
        if (debugText != null) debugText.text += s + "-";
    }

    IEnumerator TakePhotoDelayed()
    {
        //yield return new WaitForSeconds(5);
        yield return new WaitForEndOfFrame();

        while (PhotonNetwork.NetworkClientState != ClientState.Joined)
        {
            PrintMessage("Waiting to connect");
            //PhotonNetwork.ReconnectAndRejoin();
            yield return new WaitForSeconds(1);
        }

        StartPicture();
    }

    public void StartPictureDelayed()
    {
        StartCoroutine(TakePhotoDelayed());
    }

    private void Update()
    {
        if (connectionText != null) connectionText.text = PhotonNetwork.NetworkClientState.ToString();

#if UNITY_STANDALONE || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isHololens) StartPictureDelayed();
            else StartPicture();
        }
#endif
    }

    const int imageDownSampleFactor =
#if UNITY_EDITOR || UNITY_STANDALONE
        2;
#else
        8;
#endif

    public void StartPicture()
    {
        PrintMessage("Start picture called. DebugText:" + debugText);
        if (debugText == null) return;
        debugText.text = "";
        if (isHololens)
        {
#if UNITY_WSA
            //Resolution 
            cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            targetTexture = new Texture2D(cameraResolution.width / imageDownSampleFactor, cameraResolution.height / imageDownSampleFactor);

            // Create a PhotoCapture object
            PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject) {
                photoCaptureObject = captureObject;
                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.hologramOpacity = 0.0f;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

                // Activate the camera
                photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result) {
                    // Take a picture
                    PrintMessage("Taking picture");
                    photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
                    PrintMessage("Picture taken");
                });
            });
#endif
        }
        else
        {
            targetTexture = new Texture2D(webCamTexture.width, webCamTexture.height);
            targetTexture.SetPixels(webCamTexture.GetPixels());
            targetTexture.Apply();
            byte[] bytes = targetTexture.EncodeToPNG();
            view.RPC("ReceivePhoto", Photon.Pun.RpcTarget.All, webCamTexture.width, webCamTexture.height, bytes );
            //File.WriteAllBytes(your_path + "photo.png", bytes);
        }
    }

#if UNITY_WSA
    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        PrintMessage("Picture data in memory");
        // Copy the raw image data into the target texture
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);

        //PrintMessage("Setting texture to quad local");
        if (photoRendererLocal != null) photoRendererLocal.material.SetTexture("_MainTex", targetTexture);
        else if (photoImageLocal != null) photoImageLocal.texture = targetTexture;
   
        // Deactivate the camera
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);

        PrintMessage("Encoding to PNG");
        bytes = targetTexture.EncodeToPNG();

        PrintMessage("Sendind RPC (" + bytes.Length + ") (" + ((float) bytes.Length) / (cameraResolution.width * cameraResolution.height) + ")");

        view.RPC("ReceivePhoto", RpcTarget.All, cameraResolution.width / imageDownSampleFactor, cameraResolution.height / imageDownSampleFactor, bytes);

        //Thread thread = new Thread( new ThreadStart( SendRPCThread));
        //thread.Start();

        //StartCoroutine(SendRPCImagePackets(cameraResolution.width, cameraResolution.height, bytes));

        PrintMessage("RPC Sent");
    }
#endif

    [PunRPC]
    public void ReceivePhoto(int width, int height, byte[] data)
    {
        PrintMessage("Receive RPC. Applying data to texture");
        targetTexture = new Texture2D(width, height);
        targetTexture.LoadImage(data);

        PrintMessage("Setting texture to quad");
        if (photoRenderer != null) photoRenderer.material.SetTexture("_MainTex", targetTexture);
        else if (photoImage != null) photoImage.texture = targetTexture;
        PrintMessage("Texture set");
    }

    private void SendRPCThread ()
    {
        PrintMessage("ThreadStart");
        view.RPC("ReceivePhoto", RpcTarget.All, cameraResolution.width, cameraResolution.height, bytes);
        PrintMessage("ThreadEnd");
    }

    const int packetLength = 4096;

    IEnumerator SendRPCImagePackets (int width, int height, byte[] data)
    {
        int numPackets = data.Length / packetLength;
        packetBytesSend = new byte[packetLength];

        for ( int i = 0; i < numPackets; i++ )
        {
            Array.Copy ( bytes, i * packetLength, packetBytesSend, 0, packetLength);
            view.RPC("ReceiveRPCImagePackets", RpcTarget.All, i, numPackets, cameraResolution.width, cameraResolution.height, packetBytesSend);
            yield return null;
        }
    }

    [PunRPC]
    void ReceiveRPCImagePackets ( int index, int totalNum, int width, int height, byte[] data )
    {
        if (index == 0) packetBytesReceive = new byte[packetLength * totalNum];
        int currentSize = packetBytesReceive.Length;
//        Array.Resize(ref packetBytesReceive, currentSize + data.Length);
        Array.Copy(data, 0, packetBytesReceive, currentSize, packetLength);
        if ( index == totalNum - 1)
        {
            ReceivePhoto(width, height, packetBytesReceive);
        }
    }

    private void OnDisable()
    {
        // Deactivate the camera
        //photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

#if UNITY_WSA
    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown the photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
#endif
}