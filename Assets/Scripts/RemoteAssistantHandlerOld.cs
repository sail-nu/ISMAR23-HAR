#if UNITY_WSA
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.Windows.WebCam;
using System.Linq;

public class RemoteAssistantHandlerOld : MonoBehaviour
{
    public RawImage pictureImage;
    public RawImage liveWebcamImage;

    public Renderer pictureRenderer;
    public Renderer liveWebcamRenderer;

    WebCamTexture webCamTexture;
    public PhotonView view;
    Texture2D targetTexture = null;
    byte[] bytes;
    float startTime = -1;

#if UNITY_EDITOR 
    bool isHololens = false;
#else
    bool isHololens = true;
#endif

    // Hololens
    PhotoCapture photoCaptureObject = null;

    // Start is called before the first frame update
    void Start()
    {
        view = GetComponent<PhotonView>();
        if (isHololens) return;

        Debug.LogError("Starting web cam"); 
        startTime = Time.time;
        webCamTexture = new WebCamTexture();
        webCamTexture.Play();
        if (liveWebcamImage != null ) liveWebcamImage.texture = webCamTexture;
        if ( liveWebcamRenderer != null ) liveWebcamRenderer.material.SetTexture("_MainTex", webCamTexture);
    }

    public void Update()
    {
        if (startTime >= 0 && Time.time > startTime + 5)
        {
            TakeAndSendAPicture();
            startTime = -1;
        }
    }

    public void TakeAndSendAPicture ()
    {
        Debug.LogError("Taking picture");
        if (view == null) return;

        if (isHololens) StartCoroutine(TakePhoto());
        else TakePhotoNow();
    }

    IEnumerator TakePhoto()
    {
        yield return new WaitForEndOfFrame();
        TakePhotoNow();
    }

    void TakePhotoNow()
    {
        if ( isHololens)
        {
            // Create a PhotoCapture object
            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending
                ((res) => res.width * res.height).First();
            targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
            PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
            {
                photoCaptureObject = captureObject;
                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.hologramOpacity = 0.0f;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

                // Activate the camera
                photoCaptureObject.StartPhotoModeAsync(cameraParameters,
                    delegate (PhotoCapture.PhotoCaptureResult result)
                    {
                    // Take a picture
                        photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
                    });
            });
        }
        else
        {
            targetTexture = new Texture2D(webCamTexture.width, webCamTexture.height);
            targetTexture.SetPixels(webCamTexture.GetPixels());
            targetTexture.Apply();
            byte[] bytes = targetTexture.EncodeToPNG();
            view.RPC("ReceivePhoto", Photon.Pun.RpcTarget.All, bytes);
            //File.WriteAllBytes(your_path + "photo.png", bytes);
        }
    }

    [PunRPC]
    public void ReceivePhoto(byte[] data)
    {
        targetTexture.LoadImage(data);
        if (pictureImage != null) pictureImage.texture = targetTexture;
        if ( pictureRenderer != null ) pictureRenderer.material.SetTexture("_MainTex", targetTexture);
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);
        if (liveWebcamRenderer != null) liveWebcamRenderer.material.SetTexture("_MainTex", targetTexture);
        if (liveWebcamImage != null ) liveWebcamImage.texture = targetTexture;

        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    void OnStoppedPhotoMode(UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown the photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
}

#endif