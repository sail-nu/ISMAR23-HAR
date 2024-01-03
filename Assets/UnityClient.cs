using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

public class UnityClient : MonoBehaviour
{
    public GameObject textBox;
    public GameObject measurementBox;
    public GameObject partGreen;
    public GameObject partRed;
    public GameObject gageGreen;
    public GameObject gageRed;
    public GameObject record1;
    public GameObject record2;
    public GameObject record3;
    public GameObject stepTracker;
    public GameObject[] stepIndicators = new GameObject[9];

    public AudioSource audioSource;

    TextMesh textMesh;
    TextMesh textGage;
    TextMesh textRec1;
    TextMesh textRec2;
    TextMesh textRec3;
    TextMesh textSteps;
    private TcpClient client;
    private NetworkStream stream;
    private byte[] receiveBuffer = new byte[1024];
    // Names of Hand Joints
    private string[] handJoints = { "None", "Wrist", "Palm", "ThumbMetacarpalJoint", "ThumbProximalJoint", "ThumbDistalJoint", "ThumbTip", "IndexMetacarpal", "IndexKnuckle", "IndexMiddleJoint", "IndexDistalJoint", "IndexTip", "MiddleMetacarpal", "MiddleKnuckle", "MiddleMiddleJoint", "MiddleDistalJoint", "MiddleTip", "RingMetacarpal", "RingKnuckle", "RingMiddleJoint", "RingDistalJoint", "RingTip", "PinkyMetacarpal", "PinkyKnuckle", "PinkyMiddleJoint", "PinkyDistalJoint", "PinkyTip" };
    int cnt = 0;
    int interval = 1;
    char partOn = '0';
    char gageOn = '0';
    string detectedSteps = "000000000";
    StringBuilder recordSteps = new StringBuilder("0000");

    private void Start()
    {
        textMesh = textBox.GetComponent<TextMesh>();
        textMesh.text = "";
        textGage = measurementBox.GetComponent<TextMesh>();
        textGage.text = "";
        textRec1 = record1.GetComponent<TextMesh>();
        textRec1.text = "-";
        textRec2 = record2.GetComponent<TextMesh>();
        textRec2.text = "-";
        textRec3 = record3.GetComponent<TextMesh>();
        textRec3.text = "-";
        textSteps = stepTracker.GetComponent<TextMesh>();
        textSteps.text = "";

        

        string serverAddress = "127.0.0.1";  // Replace with the server address
        int serverPort = 8888;  // Replace with the server port

        try
        {
            // Connect to the server
            client = new TcpClient(serverAddress, serverPort);
            stream = client.GetStream();
            Debug.Log("Connected to the Python server.");
        }
        catch (Exception e)
        {
            Debug.LogError("Error connecting to the server: " + e.Message);
        }
    }

    private void Update()
    {
        // Example code to send a tuple of three float values
        /*if (Input.GetKeyDown(KeyCode.Space))
        {
            //SendData("(1.23, 2.44, 3.43)");
            LogHand();
        }*/

        if (cnt % interval == 0)
        {
            cnt = 1;
            LogHand();
            updateStepTracker();
        }

        cnt++;
    }

    private void updateStepTracker()
    {
        for (int i = 0; i < 9; i++)
        {
            if (detectedSteps[i] == '0')
            {
                stepIndicators[i].GetComponent<MeshRenderer>().material.color = Color.gray;
            }

            else if (detectedSteps[i] == '1')
            {
                stepIndicators[i].GetComponent<MeshRenderer>().material.color = Color.green;
            }

            else if (detectedSteps[i] == '2')
            {
                stepIndicators[i].GetComponent<MeshRenderer>().material.color = Color.red;
            }

        }
    }

    private void switchPartLight()
    {
        if (partOn == '1')
        {
            partOn = '0';
            partGreen.SetActive(false);
            partRed.SetActive(true);
        } 
        else
        {
            partOn = '1';
            partGreen.SetActive(true);
            partRed.SetActive(false);
        }
    }

    private void switchGageLight()
    {
        if (gageOn == '1')
        {
            gageOn = '0';
            gageGreen.SetActive(false);
            gageRed.SetActive(true);
        }
        else
        {
            gageOn = '1';
            gageGreen.SetActive(true);
            gageRed.SetActive(false);
        }
    }

    private void LogHand()
    {
        Debug.Log("Logging Hand Data");

        string data = "(";


        for (int i = 1; i < handJoints.Length; i++)
        {
            if (HandJointUtils.TryGetJointPose((TrackedHandJoint)i, Handedness.Right, out MixedRealityPose pose))
            {
                if (i == 1)
                    data += pose.Position.x;
                else
                    data += ", " + pose.Position.x;
                data += ", " + pose.Position.y;
                data += ", " + pose.Position.z;
            }

            else
            {
                if (i == 1)
                    data += "0";
                else
                    data += ", " + "0";
                data += ", " + "0";
                data += ", " + "0";
            }

        }

        data += ")";

        SendData(data);
   
    }


    private void SendData(string data)
    {
        data = data + recordSteps;
        // Convert the tuple of float values to a string
        //string data = string.Format("({0}, {1}, {2})", value1, value2, value3);
        byte[] dataBytes = Encoding.ASCII.GetBytes(data);

        try
        {
            // Send the data to the Python server
            stream.Write(dataBytes, 0, dataBytes.Length);
            Debug.Log("Data sent to the Python server.");

            // Receive the result from the Python server
            int bytesRead = stream.Read(receiveBuffer, 0, receiveBuffer.Length);
            string result = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
            Debug.Log("Received result: " + result);

            char partSignal = result[result.Length - 2];
            char gageSignal = result[result.Length - 1];

            if (partSignal != partOn)
            {
                switchPartLight();
            }

            if (gageSignal != gageOn)
            {
                switchGageLight();
            }

            textMesh.text = result.Substring(0, result.Length - 20);
            textGage.text = result.Substring(result.Length - 20, 9);
            detectedSteps = result.Substring(result.Length - 11, 9); 
            textSteps.text = result.Substring(result.Length - 11, 9);
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending/receiving data: " + e.Message);
        }
    }

    public void logMeasurements()
    {
        Debug.Log("Logging Gage Measurement");
        if (textRec1.text == "-")
        {
            recordSteps[0] = '1';
            LogHand();
            if (detectedSteps[5] == '1')
            {
                textRec1.text = textGage.text;
            }
            recordSteps[0] = '0';
        }
        else if (textRec2.text == "-")
        {
            recordSteps[1] = '1';
            textRec2.text = textGage.text;
        }
        else if (textRec3.text == "-")
        {
            recordSteps[2] = '1';
            textRec3.text = textGage.text;
        }
    }

    public void submitMeasurement()
    {
        recordSteps[3] = '1';
    }

    private void OnDestroy()
    {
        // Close the TCP connection when the Unity app is closed
        if (stream != null)
        {
            stream.Close();
        }

        if (client != null)
        {
            client.Close();
        }
    }
}
