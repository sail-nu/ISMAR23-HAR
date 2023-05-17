using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class DataLogger : MonoBehaviour
{
    public ModuleController moduleController;
    // File Properties
    private string eyetrackingPathEnd = "eye-tracking.csv";
    private string headtrackingPathEnd = "head-tracking.csv";
    private string handtrackingPathEnd = "hand-tracking.csv";
    private string interactionPathEnd = "interaction.csv";
    private string stepPathEnd = "step.csv";
    private string metadataPathEnd = "metadata.csv";

    private string eyetrackingFileHeader = "Timestamp, DateTime, User ID, Frame, Gaze Origin x, Gaze Origin y, Gaze Origin z, Gaze Direction x, Gaze Direction y, Gaze Direction Z, Object In Gaze";
    private string headtrackingFileHeader = "Timestamp, DateTime, User ID, Frame, Euler Head Rotation x, Euler Head Rotation y, Euler Head Rotation z, Quartenion Head Rotation x, Quartenion Head y, Quartenion Head Z";
    private string handtrackingFileHeader = "Timestamp, DateTime, User ID, Frame";
    private string interactionFileHeader = "Timestamp, DateTime, User ID, Frame, Object, Interaction Start Time, Interaction End Time";
    private string stepFileHeader = "Timestamp, DateTime, User ID, Frame, Char, Step";
    private string metadataFileHeader = "Experiment Name, First Name, Last Name, User ID, Start Time, End Time, Data Path";

    // Logging Variables
    private bool sessionStarted = false;

    private string experimentName = "NSF";
    private string dateTimeFormat = "MM/dd/yyyy HH:mm:ss:ffff";
    private string folderSuffixFormat = "MM-dd-yyyy-HH-mm-ss";

    private string uniqueUserId;
    private string firstName;
    private string lastName;
    private System.DateTime sessionStartTime;
    private System.DateTime sessionEndTime;

    private List<string> eyetrackingData;
    private List<string> headtrackingData;
    private List<string> handtrackingData;
    private List<string> interactionData;
    private List<string> stepData;
    private List<string> metadata;

    // Logging Configurations
    private float samplingRate = 20f; //Hz
    private int dataRate = 500;

    // Names of Hand Joints
    private string[] handJoints = { "None", "Wrist", "Palm", "ThumbMetacarpalJoint", "ThumbProximalJoint", "ThumbDistalJoint", "ThumbTip", "IndexMetacarpal", "IndexKnuckle", "IndexMiddleJoint", "IndexDistalJoint", "IndexTip", "MiddleMetacarpal", "MiddleKnuckle", "MiddleMiddleJoint", "MiddleDistalJoint", "MiddleTip", "RingMetacarpal", "RingKnuckle", "RingMiddleJoint", "RingDistalJoint", "RingTip", "PinkyMetacarpal", "PinkyKnuckle", "PinkyMiddleJoint", "PinkyDistalJoint", "PinkyTip" };


    void Start()
    {
        StartUserSession("first", "last");
        metadata = new List<string>();
        // SendData(Application.persistentDataPath);
        MakeHandTrackingHeader();
    }

    void Update()
    {
        LogUserData();

        if (Time.frameCount % dataRate == 0) SaveData();
    }

    private void MakeHandTrackingHeader()
    {
        for (int i = 1; i < handJoints.Length; i++)
        {
            handtrackingFileHeader += "," + handJoints[i] + "_l_pose_x";
            handtrackingFileHeader += "," + handJoints[i] + "_l_pose_y";
            handtrackingFileHeader += "," + handJoints[i] + "_l_pose_z";
            handtrackingFileHeader += "," + handJoints[i] + "_l_rot_x";
            handtrackingFileHeader += "," + handJoints[i] + "_l_rot_y";
            handtrackingFileHeader += "," + handJoints[i] + "_l_rot_z";
            handtrackingFileHeader += "," + handJoints[i] + "_l_rot_w";
        }

        for (int i = 1; i < handJoints.Length; i++)
        {
            handtrackingFileHeader += "," + handJoints[i] + "_r_pose_x";
            handtrackingFileHeader += "," + handJoints[i] + "_r_pose_y";
            handtrackingFileHeader += "," + handJoints[i] + "_r_pose_z";
            handtrackingFileHeader += "," + handJoints[i] + "_r_rot_x";
            handtrackingFileHeader += "," + handJoints[i] + "_r_rot_y";
            handtrackingFileHeader += "," + handJoints[i] + "_r_rot_z";
            handtrackingFileHeader += "," + handJoints[i] + "_r_rot_w";
        }

        handtrackingFileHeader += ",Label";
    }

    private void OnApplicationQuit()
    {
        EndUserSession();
        WriteDataToCSV(Application.persistentDataPath + Path.DirectorySeparatorChar + metadataPathEnd, metadataFileHeader, metadata);
    }

    public void SaveData()
    {
        EndUserSession();
        WriteDataToCSV(Application.persistentDataPath + Path.DirectorySeparatorChar + metadataPathEnd, metadataFileHeader, metadata);
    }

    public void StartUserSession(string firstname, string lastname)
    {
        firstName = firstname;
        lastName = lastname;
        sessionStartTime = System.DateTime.Now;

        // Generate new User ID
        uniqueUserId = System.Guid.NewGuid().ToString();

        ClearUserDataFromPreviousSession();

        sessionStarted = true;
        Debug.Log("Starting logging for: " + uniqueUserId);

        //StartCoroutine(LogUserData());
        //LogUserData();
    }

    public void EndUserSession()
    {
        Debug.Log("Stopping logging for: " + uniqueUserId);

        // Stop Logging Data
        //StopCoroutine(LogUserData());
        LogUserData();

        sessionEndTime = System.DateTime.Now;
        // Create new directory for user
        Directory.CreateDirectory(Application.persistentDataPath + Path.DirectorySeparatorChar + sessionStartTime.ToString(folderSuffixFormat));
        //Directory.CreateDirectory(Application.persistentDataPath + Path.DirectorySeparatorChar + uniqueUserId);
        //Directory.CreateDirectory(Application.persistentDataPath + "2" + uniqueUserId);

        Debug.Log(Application.persistentDataPath + Path.DirectorySeparatorChar + uniqueUserId);

        // Write to CSV Files
        // string folderPath = Application.persistentDataPath + Path.DirectorySeparatorChar + uniqueUserId + sessionStartTime.ToString(folderSuffixFormat) + Path.DirectorySeparatorChar;
        string folderPath = Application.persistentDataPath + Path.DirectorySeparatorChar + sessionStartTime.ToString(folderSuffixFormat) + Path.DirectorySeparatorChar;
        WriteDataToCSV(folderPath + eyetrackingPathEnd, eyetrackingFileHeader, eyetrackingData);
        WriteDataToCSV(folderPath + headtrackingPathEnd, headtrackingFileHeader, headtrackingData);
        WriteDataToCSV(folderPath + handtrackingPathEnd, handtrackingFileHeader, handtrackingData);
        WriteDataToCSV(folderPath + interactionPathEnd, interactionFileHeader, interactionData);
        WriteDataToCSV(folderPath + stepPathEnd, stepFileHeader, stepData);
        string metadataInfo = experimentName + "," + firstName + "," + lastName + "," + uniqueUserId + "," + sessionStartTime.ToString(dateTimeFormat) + "," + sessionEndTime.ToString(dateTimeFormat) + "," + Application.persistentDataPath + "/" + uniqueUserId;
        if (metadata != null) metadata.Add(metadataInfo);
    }

    private void ClearUserDataFromPreviousSession()
    {
        eyetrackingData = new List<string>();
        headtrackingData = new List<string>();
        handtrackingData = new List<string>();
        interactionData = new List<string>();
        stepData = new List<string>();
    }

    /* private IEnumerator LogUserData()
    {
		float waitTime = 1/samplingRate;
        for (; ; )
        {
            LogGaze();
            LogHead();
            yield return new WaitForSecondsRealtime(waitTime);
        }
    } */

    private void LogUserData()
    {
        LogGaze();
        LogHead();
        LogHand();
    }

    private void LogGaze()
    {
        Debug.Log("Logging Gaze Data");

        //  Check for object in gaze
        string objectInGaze = "";

        if (CoreServices.InputSystem.EyeGazeProvider.GazeTarget && CoreServices.InputSystem.EyeGazeProvider.GazeTarget.GetComponent<Loggable>())
        {
            objectInGaze = CoreServices.InputSystem.EyeGazeProvider.GazeTarget.GetComponent<Loggable>().GetDescription();
        }

        string data = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString() + "," +
                      System.DateTime.Now.ToString(dateTimeFormat) + "," +
                      uniqueUserId + "," +
                      Time.frameCount.ToString() + "," +
                      CoreServices.InputSystem.EyeGazeProvider.GazeOrigin.x.ToString() + "," +
                      CoreServices.InputSystem.EyeGazeProvider.GazeOrigin.y.ToString() + "," +
                      CoreServices.InputSystem.EyeGazeProvider.GazeOrigin.z.ToString() + "," +
                      CoreServices.InputSystem.EyeGazeProvider.GazeDirection.x.ToString() + "," +
                      CoreServices.InputSystem.EyeGazeProvider.GazeDirection.y.ToString() + "," +
                      CoreServices.InputSystem.EyeGazeProvider.GazeDirection.z.ToString() + "," +
                      objectInGaze;

        // Debug.Log("Gaze->"+data);
        if (eyetrackingData != null) eyetrackingData.Add(data);
    }

    private void LogHead()
    {
        Debug.Log("Logging Head Data");

        Quaternion headQuat = Camera.main.transform.rotation;
        Vector3 headEuler = Camera.main.transform.eulerAngles;

        string data = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString() + "," + 
                      System.DateTime.Now.ToString(dateTimeFormat) + "," +
                      uniqueUserId + "," +
                      Time.frameCount.ToString() + "," +
                      headQuat.x.ToString() + "," +
                      headQuat.y.ToString() + "," +
                      headQuat.z.ToString() + "," +
                      headEuler.x.ToString() + "," +
                      headEuler.y.ToString() + "," +
                      headEuler.z.ToString();

        // Debug.Log("Head->"+data);
        headtrackingData.Add(data);
    }

    private void LogHand()
    {
        Debug.Log("Logging Hand Data");

        string data = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString() + "," + 
                      System.DateTime.Now.ToString(dateTimeFormat) + "," +
                      uniqueUserId + "," +
                      Time.frameCount.ToString();

        for (int i = 1; i < handJoints.Length; i++)
        {
            if (HandJointUtils.TryGetJointPose((TrackedHandJoint)i, Handedness.Left, out MixedRealityPose pose))
            {
                data += "," + pose.Position.x;
                data += "," + pose.Position.y;
                data += "," + pose.Position.z;
                data += "," + pose.Rotation.x;
                data += "," + pose.Rotation.y;
                data += "," + pose.Rotation.z;
                data += "," + pose.Rotation.w;
            }

            else
            {
                data += ",,,,,,,";
            }
        }

        for (int i = 1; i < handJoints.Length; i++)
        {
            if (HandJointUtils.TryGetJointPose((TrackedHandJoint)i, Handedness.Right, out MixedRealityPose pose))
            {
                data += "," + pose.Position.x;
                data += "," + pose.Position.y;
                data += "," + pose.Position.z;
                data += "," + pose.Rotation.x;
                data += "," + pose.Rotation.y;
                data += "," + pose.Rotation.z;
                data += "," + pose.Rotation.w;
            }

            else
            {
                data += ",,,,,,,";
            }

        }
        data += "," + moduleController.getStepIdx();
        if (moduleController.getStepIdx() == -1)
            Debug.Log("Cancel!!!");
        Debug.Log("Step Number = "+moduleController.getStepIdx());
        // Debug.Log("Hand->"+data);
        handtrackingData.Add(data);
    }

    public void LogInteraction(string objectName, string startTime, string endTime)
    {
        if (!sessionStarted)
        {
            return;
        }

        string data = System.DateTime.Now.ToString(dateTimeFormat) + "," +
                      uniqueUserId + "," +
                      Time.frameCount.ToString() + "," +
                      objectName + "," +
                      startTime + "," +
                      endTime;

        // Debug.Log("Interaction->"+data);
        interactionData.Add(data);
    }

    public void LogStep(string moduleIdx, string stepIdx)
    {
        Debug.Log("Logging Step Data");


        string data = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString() + "," +
                      System.DateTime.Now.ToString(dateTimeFormat) + "," +
                      uniqueUserId + "," +
                      Time.frameCount.ToString() + "," +
                      moduleIdx + "," +
                      stepIdx;
                      ;

        // Debug.Log("Gaze->"+data);
        if (stepData != null) stepData.Add(data);
    }

    private void WriteDataToCSV(string savePath, string csvHeader, List<string> data)
    {
        Debug.Log("Save File->" + savePath);
        // SendData(savePath);
        // Write Header
        TextWriter tw = new StreamWriter(savePath, false); // Note: Setting to false deletes previous contents at the file path
        tw.WriteLine(csvHeader);
        tw.Close();

        tw = new StreamWriter(savePath, true); // Note: Setting to true preserves previous contents at the file path

        // Write Data
        if (data != null) for (int i = 0; i < data.Count; i++)
            {
                tw.WriteLine(data[i]);
            }

        tw.Close();
    }

    public void SendData(string enteredText)
    {
        string participantID = "P01";

        StartCoroutine(PostToQualtrics(participantID, enteredText));
    }

    IEnumerator PostToQualtrics(string PID, string TextEntered)
    {
        string responseURL = "https://neu.co1.qualtrics.com/jfe/form/SV_8Hc3bjkGjW2QbgG" + "?" + "PID=" + PID + "&" + "TextEntered=" + TextEntered;

        UnityWebRequest www = new UnityWebRequest(responseURL);

        yield return www.SendWebRequest();
    }

}
