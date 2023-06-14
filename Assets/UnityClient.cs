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
    TextMesh textMesh;
    private TcpClient client;
    private NetworkStream stream;
    private byte[] receiveBuffer = new byte[1024];
    // Names of Hand Joints
    private string[] handJoints = { "None", "Wrist", "Palm", "ThumbMetacarpalJoint", "ThumbProximalJoint", "ThumbDistalJoint", "ThumbTip", "IndexMetacarpal", "IndexKnuckle", "IndexMiddleJoint", "IndexDistalJoint", "IndexTip", "MiddleMetacarpal", "MiddleKnuckle", "MiddleMiddleJoint", "MiddleDistalJoint", "MiddleTip", "RingMetacarpal", "RingKnuckle", "RingMiddleJoint", "RingDistalJoint", "RingTip", "PinkyMetacarpal", "PinkyKnuckle", "PinkyMiddleJoint", "PinkyDistalJoint", "PinkyTip" };
    int cnt = 0;
    int interval = 1;

    private void Start()
    {
        textMesh = textBox.GetComponent<TextMesh>();
        textMesh.text = "";
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
        }

        cnt++;
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
            textMesh.text = result;
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending/receiving data: " + e.Message);
        }
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
