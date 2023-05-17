using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.Video;
//using TriLib;
//using Siccity.GLTFUtility;

public class AutoFillScene : MonoBehaviour
{
    public Transform animationsParent;
    public Transform modulesParent;

    public VideoPlayer videoPlayer;
    public RawImage rawImage; 
    public Renderer imageRenderer;
    public AudioSource audioPlayer;
    //public AssetDownloader trilibLoader;

    Step testStep;

    public string moduleDataPath = "Powder_Feeder";
    string animationsPath;
    string imagesPath;
    string videosPath;
    // string soundsPath;
    string textFile;
    
    const int ANIMATIONS = 1;
    const int AUDIO = 2; 
    const int VIDEOS = 3;
    const int IMAGES = 4;

    Dictionary<string, Dictionary<string, Step>> machineDatabase;
    Dictionary <string, GameObject> moduleGameObjects = new Dictionary<string, GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        moduleDataPath = "Assets/Resources/" + moduleDataPath;
        animationsPath = moduleDataPath + "/Animations/";
        imagesPath = moduleDataPath + "/Images/";
        videosPath = moduleDataPath + "/Videos/";
        // soundsPath = moduleDataPath + "/Sounds/";
        textFile = moduleDataPath + "/moduleTextData.csv";

        //Debug.LogError("Class:" + GetType().Name);

        /* To test loading
        testStep = gameObject.AddComponent<Step>();
        GameObject animation = Resources.Load<GameObject>("Powder_Feeder/Animations/01_DIS_Disassembling/06_DIS_Remove_Bolts"); //  Path.Combine(animationsPath, "01_DIS_Disassembling/06_DIS_Remove_Bolts"));
        testStep.anim = animation;
        if ( animation != null ) Instantiate(animation, animationsParent);

        testStep.images = new Sprite[1];
        testStep.images[0] = Resources.Load<Sprite>("Powder_Feeder/Images/01_DIS_Disassembling/06_DIS_Remove_Bolts");//  Path.Combine(imagesPath, "01_DIS_Disassembling/06_DIS_Remove_Bolts"));
        //rawImage.texture = step.images[0].texture;
        imageRenderer.material.SetTexture("_MainTex", testStep.images[0].texture);

        testStep.videos = new VideoClip[1];
        testStep.videos[0] = Resources.Load<VideoClip>("Powder_Feeder/Videos/01_DIS_Disassembling/06_DIS_Remove_Bolts");// Path.Combine(videosPath, "01_DIS_Disassembling/06_DIS_Remove_Bolts"));
        videoPlayer.clip = testStep.videos[0];

        AudioClip[] audioClips = new AudioClip[1];
        audioClips[0] = Resources.Load<AudioClip>("Powder_Feeder/Sounds/01_DIS_Disassembling/06_DIS_Remove_Bolts");// Path.Combine(soundsPath, "06_DIS_Remove_Bolts"));
        audioPlayer.clip = audioClips[0];
        
        ProcessTextData("Assets/Resources/Powder_Feeder/Powder Feeder AR-Table 1.csv");//   textFile);
        */

        CreateSceneGraph();
    }

    void CreateSceneGraph()
    {
        // fill the animations
        ProcessMediaData(animationsPath, ANIMATIONS);

        // fill the video
        ProcessMediaData(videosPath, VIDEOS);

        // fill the images
        ProcessMediaData(imagesPath, IMAGES);

        // fill the sounds
        //ProcessMediaData(soundPath, AUDIO);

        // fill the text 
        ProcessTextData(textFile);
    }

    Step CreateStep( string stepName, GameObject moduleGO )
    {
        // create step gameobjects
        GameObject stepGO = new GameObject(stepName);
        stepGO.transform.SetParent(moduleGO.transform);
        return stepGO.AddComponent<Step>();
    }

    void ProcessMediaData (string path, int type)
    {
        string moduleName, stepName, stepShortName, resourcePath; // moduleShortName
        string[] moduleDirectories = GetDirectoriesEntries(path);
        //Dictionary<string, Step> stepDatabase = null;
        //GameObject moduleGO = null;

        foreach (string modulePath in moduleDirectories)
        {
            moduleName = modulePath.Substring(modulePath.LastIndexOf('/') + 1);
            if (moduleName[2] != '_') continue;
            //moduleShortName = moduleName.Substring(0, 2);

            //if ( type == SCENE )
            //{
            //    moduleGO = new GameObject(moduleName);
            //    moduleGO.transform.SetParent(transform);

            //    if (machineDatabase == null) machineDatabase = new Dictionary<string, Dictionary<string, Step>>();
            //    stepDatabase = new Dictionary<string, Step>();
            //}

            string[] stepFiles = GetFilesEntries( modulePath );
            for (int i = 0; i < stepFiles.Length; i++) stepFiles[i] = stepFiles[i].Replace("\\", "/");
            int laststepIndex = 0;

            foreach (string stepPath in stepFiles)
            {
                if (stepPath.EndsWith(".meta") || stepPath.EndsWith(".fbx")) continue;
                stepName = stepPath.Substring(stepPath.LastIndexOf('/') + 1);
                stepShortName = stepName.Substring(0, 2);
                resourcePath = stepPath.Substring(17);
                resourcePath = resourcePath.Substring ( 0, resourcePath.LastIndexOf('.'));//  .Substring(stepPath.IndexOf('/') + 1);

                switch ( type )
                {
                    case ANIMATIONS:
                        // load animations and link them to object
                        GameObject animation = Resources.Load<GameObject>(resourcePath);
                        //GameObject animation = Resources.Load<GameObject>("Powder_Feeder/Animations/01_DIS_Disassembling/06_DIS_Remove_Bolts");
                        GameObject stepObject = getStepFromModuleAndStepNames(moduleName, stepShortName).anim = Instantiate(animation, animationsParent);
                        RemoveVRParts.RecursivelyDisableVR_Children(stepObject.transform);
//                        stepObject.SetActive(false);
                        break;
                    case IMAGES:
                        Sprite[] sprites = new Sprite[1];
                        sprites[0] = Resources.Load<Sprite>(resourcePath);
                        //if (sprites[0] = null) sprites[0] =
                        //Texture2D t = Resources.Load<Texture2D>(resourcePath);
                        getStepFromModuleAndStepNames(moduleName, stepShortName).images = sprites;
                        //imageRenderer.material.SetTexture("_MainTex", sprites[0].texture);
                        break;
                    case VIDEOS:
                        VideoClip[] videoClips = new VideoClip[1];
                        videoClips[0] = Resources.Load<VideoClip>(resourcePath);
                        getStepFromModuleAndStepNames(moduleName, stepShortName).videos = videoClips;
                        if ( videoPlayer.clip == null ) videoPlayer.clip = videoClips[0];
                        break;
                    //case AUDIO:
                    //    AudioClip[] audioClips = new AudioClip[1];
                    //    audioClips[0] = Resources.Load<AudioClip>(resourcePath);
                    //    getStepFromModuleAndStepNames(moduleName, stepShortName).audioClips = audioClips;
                    //    break;
                    //case SCENE:
                    //    int stepIndex = -1;
                    //    int.TryParse(stepShortName, out stepIndex);
                    //    if (stepIndex == -1) continue;

                    //    string indexString;
                    //    while (stepIndex - laststepIndex > 1)
                    //    {
                    //        laststepIndex++;
                    //        indexString = laststepIndex.ToString("00");
                    //        stepDatabase.Add(indexString, CreateStep(indexString, moduleGO));
                    //    }
                    //    laststepIndex = stepIndex;

                    //    stepDatabase.Add(stepShortName, CreateStep(stepShortName, moduleGO));
                    //    break;
                }

            }
            //if ( type == SCENE) machineDatabase.Add(moduleName, stepDatabase);
        }
    }

    void ProcessTextData(string path)
    {
        string currentStepShortName = null;
        string currentModuleName = null;
        Step currentStep;
        //int counter = 0;
        bool firstLine = true;

        string csvString = GetTextFileString(path);
        string[] csvLines = csvString.Split('\n');
        foreach ( string line in csvLines )
        {
            if ( firstLine )
            {
                firstLine = false;
                continue;
            }

            string[] fields = line.Split(',');
            if ( fields == null || fields.Length < 2 || fields[1] == "") continue;

            for ( int i = 0; i < fields.Length; i++ )
            {
                fields[i] = fields[i].Replace(';', ',');
            }

            //Module,Step,Step Text, Show Warning,Warning,Ask for Confirmation, Confirmation, Picture, Youtube Video Link, Additional Model Required, Video Timestamp References, AR Fbx Drive Links, Maya Drive Links, Additional Information, Preceding Step, Questions Associated with Step(?),,
            if (fields[0] != "") currentModuleName = fields[0];//.Substring(0, 2);
            if (fields[1].Length < 2)
            {
                Debug.LogError("Step name [" + fields[1] + "] is not including at least a digit index at the front in the line [" + line + "] so is the step name missing in the CSV file?");
                currentStepShortName = fields[1];
            }
            else
                currentStepShortName = fields[1].Substring(0, 2);

            if (fields.Length < 9)
            {
                Debug.LogError("ProcessLine:" + line + " does not have 9 elements needed, only " + fields.Length + " . May be there are some commas in the original XLS file that should be replace with semi-colons, OR some new lines passed inside a text element instead of a continuous line of text");
            }

            currentStep = getStepFromModuleAndStepNames(currentModuleName, currentStepShortName);
            //currentStep = testStep;

            currentStep.text = fields[2];
            //currentStep.displayWarning = fields[3].Equals("Yes");
            currentStep.warningText = fields[4];
            //currentStep.askForConfirmation = fields[5].Equals("Yes");
            currentStep.confirmationText = fields[6];
            currentStep.moduleStepYesConfirmation = fields[7];

            currentStep.moduleStepNoConfirmation = fields[8];

            //counter++;
            //if ( counter == 5 ) return;
            //currentStep.stepToLoadOnNo = ?
            //currentStep.stepToLoadOnYes = ?
        }
    }

    Step getStepFromModuleAndStepNames ( string moduleName, string stepShortName )
    {
        GameObject moduleGO = null;
        string moduleShortName;
        if (moduleName.Length < 2)
        {
            Debug.LogError("Module name [" + moduleName + "] is not including at least a digit index at the front so is the module name missing in the CSV file?");
            moduleShortName = moduleName;
        }
        else moduleShortName = moduleName.Substring(0, 2);
        
        if (machineDatabase == null) machineDatabase = new Dictionary<string, Dictionary<string, Step>>();

        if (!machineDatabase.ContainsKey(moduleShortName))
        {
            machineDatabase[moduleShortName] = new Dictionary<string, Step>();
            moduleGO = new GameObject(moduleName);
            moduleGO.transform.SetParent(modulesParent);
            moduleGameObjects.Add(moduleShortName, moduleGO);
        }
        else moduleGO = moduleGameObjects[moduleShortName];

        if ( machineDatabase[moduleShortName].ContainsKey(stepShortName)) return machineDatabase[moduleShortName][stepShortName];

        Dictionary < string, Step > stepDatabase = machineDatabase[moduleShortName];
        if (stepDatabase == null) stepDatabase = new Dictionary<string, Step>();

        int stepIndex = -1;
        int lastStepIndex = stepDatabase.Count;
        int.TryParse(stepShortName, out stepIndex);
        if (stepIndex == -1) return null;
        string indexString;
        while (stepIndex - lastStepIndex > 1)
        {
            lastStepIndex++;
            indexString = lastStepIndex.ToString("00");
            stepDatabase.Add(indexString, CreateStep(indexString, moduleGO));
        }

        if ( moduleGO == null )
        {
            for (int i = 0; i < transform.childCount; i++) if (transform.GetChild(i).name.Substring(0, 2).Equals(moduleShortName)) moduleGO = transform.GetChild(i).gameObject;
        }
        Step ret = CreateStep(stepShortName, moduleGO);
        stepDatabase.Add(stepShortName, ret );
        machineDatabase[moduleShortName] = stepDatabase;
        return ret;
        //return machineDatabase[moduleShortName][stepShortName];
    }

    string[] GetFilesEntries(string path)
    {
        if (!Directory.Exists(path))
        {
            Debug.LogError("Cannot find " + path);
            return null;
        }
        return Directory.GetFiles(path);
    }

    string[] GetDirectoriesEntries(string path)
    {
        if (!Directory.Exists(path))
        {
            Debug.LogError("Cannot find " + path);
            return null;
        }
        return Directory.GetDirectories(path);
    }

    string GetTextFileString( string f )
    {
        string rawstreamdata = "";

        if (!File.Exists(f))
        {
            Debug.LogError("ReadSensorFile:Cannot find " + f);
            Application.Quit();
            return null;
        }

        StreamReader reader = new StreamReader(f);
        rawstreamdata = reader.ReadToEnd();
        reader.Close();

        //rawstreamdata = File.ReadAllText(f);

        //string substring = rawstreamdata.Substring(128, 256);
        //Debug.LogError("ReadSensorFile:Data:" + substring);

        return rawstreamdata;
    }
}
