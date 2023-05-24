using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using UnityEngine;
#if ENABLE_MULTIUSERS
using Photon.Pun;
using Photon.Realtime;
#endif


public class ModuleController : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] GameObject mainMenuObjects;

    [Header("Char Menu")]
    [SerializeField] GameObject charMenuObjects;
    [SerializeField] GameObject stepMenuObjects;

    [Header("Module Panel")]
    [SerializeField] GameObject modulePanel;
    [SerializeField] TMPro.TMP_Text panelTitle;
    [SerializeField] TMPro.TMP_Text panelText;
    [SerializeField] GameObject nextButton;
    [SerializeField] GameObject backButton;
    [SerializeField] GameObject playButton;

    [Header("Additional Information")]
    [SerializeField] GameObject additionalInformationPanel;
    [SerializeField] TMPro.TMP_Text additionalInformationText;

    [Header("Model Object Collection")]
    [SerializeField] GameObject partPanel;
    [SerializeField] ScrollingObjectCollection scrollingObjectCollection;
    [SerializeField] GridObjectCollection gridObjectCollection;
    private Transform gridObjectCollectionTransform;
    [SerializeField] GameObject cardPrefab;

    [Header("Other Panels")]
    [SerializeField] GameObject animationPanel;
    [SerializeField] VideoPanelController videoPanelController;
    [SerializeField] GameObject videoButton;
    [SerializeField] ImagePanelController imagePanelController;
    [SerializeField] GameObject photoButton;
    [SerializeField] GameObject recordingButton;

    [Header("Confirmation Dialog")]
    [SerializeField]
    private GameObject dialogPrefabSmall;
    public GameObject DialogPrefabSmall
    {
        get => dialogPrefabSmall;
        set => dialogPrefabSmall = value;
    }

    [Header("Warning Messages")]
    [SerializeField] GameObject warningPanel;
    [SerializeField] TMPro.TMP_Text warningText;
    [SerializeField] GameObject secondWarningPanel;
    [SerializeField] TMPro.TMP_Text secondWarningText;

    [SerializeField] GameObject operationManual;


    [Header("Audio")]
    [SerializeField] AudioClip startAudio;
    [SerializeField] AudioClip stopAudio;


    // Class Variables
    private Step[] steps;
    private Step currentStep;
    private int currentStepIdx = 0;
    private int currentModuleIdx = 0; 
    private int firstStepInModuleIdx = 0;
    private bool displayingConfirmationDialog = false;
    private string moduleStepCombinedNameToLoad;

    AudioSource audioPlayer;

    float startTime = -1;
    bool mobileUIShown = true;

    public bool startModule;
    public GameObject baseVRModel;
    public bool hideMachineWhenMarkerNotDetected =  true; // this will hide the augmentation when no marker is detected 

    public DataLogger dataLogger;

    public bool recording = true;
    bool cancel = false;
    int cancelCnt = 10; 

#if ENABLE_MULTIUSERS
    public PhotonView view;
#endif

    private Vector3 modelScaleFactor = new Vector3(0.0001f, 0.0001f, 0.0001f);


    void Start()
    {
        RemoveAllPanelsFromScene();
//#if UNITY_IOS || UNITY_ANDROID
//        //transform.parent.GetComponent<DefaultObserverEventHandler>().enabled = false;
//        LoadModuleAndStep("01.06"); // force load step 06_DIS
//        HideMainMenu();
//#else
        
        ShowMainMenu();
//#endif


#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID
        if ( baseVRModel != null ) baseVRModel.SetActive(true);
#else
        if ( baseVRModel != null ) baseVRModel.SetActive(false);
#endif

        // This does not work, we will need to add a photonView for now in editor and link itn to the property
#if ENABLE_MULTIUSERS
//                if (view == null)
//                {
//                    view = gameObject.GetComponent<PhotonView>();
//                    if ( view == null ) view = gameObject.AddComponent<PhotonView>();
//                }
#endif
        //HandleHandMenuModuleSelection("03.04");

    }

    private void Awake()
    {
#if UNITY_IOS || UNITY_ANDROID
        // on mobile disable the marker mesh remover 
        transform.parent.GetComponent<DefaultObserverEventHandler>().enabled = false;
#else

        //transform.parent.GetComponent<DefaultObserverEventHandler>().enabled = hideMachineWhenMarkerNotDetected;
#endif
            //gridObjectCollectionTransform = gridObjectCollection.transform;

            audioPlayer = GetComponent<AudioSource>();
    }

    public void ResetUI()
    {
        videoPanelController.transform.localPosition = new Vector3(0.9f, 0.856f, 0.53f);
        videoPanelController.transform.localRotation = Quaternion.identity;
        imagePanelController.transform.localPosition = new Vector3(0.9f, 1.5f, 0.53f);
        imagePanelController.transform.localRotation = Quaternion.identity;
        modulePanel.transform.localPosition = new Vector3(0.0f, 1.5f, 0.53f);
        modulePanel.transform.localRotation = Quaternion.identity;
        operationManual.transform.localPosition = new Vector3(-0.813f, 1.5f, 0.545f);
        operationManual.transform.localRotation = Quaternion.identity;
    }

    /*
     *  Used to load modules from hand menu.
     */
    public void HandleHandMenuModuleSelection(string moduleStepCombinedName)
    {
        //HideMainMenu();

        // Confirm user would like to load scene
        moduleStepCombinedNameToLoad = moduleStepCombinedName;

#if UNITY_ANDROID || UNITY_IOS
        LoadModuleAndStep(moduleStepCombinedName);
#else
        DisplayLoadModuleConfirmationDialog();
#endif
    }

    public void Update()
    {
        LogStep(currentModuleIdx, currentStepIdx);

        if (startModule || Input.GetKeyDown(KeyCode.L))
        {
            startModule = false;
            //HandleHandMenuModuleSelection("01.05");// Powder_Feeder_Disassemble");
            LoadModuleAndStep("01.05");
            HideMainMenu();
        }

        if ((Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended) || Input.GetMouseButtonUp(0))
        {
            mobileUIShown = !mobileUIShown;

            if (startTime > 0)
            {

                if (Time.time - startTime < 0.5)
                {
                    modulePanel.transform.localPosition = new Vector3(modulePanel.transform.localPosition.x > -0.225f ? -0.225f : -0.095f, 0.044f, 0.264885f);
                    if (mobileUIShown)
                    {
                        if (operationManual != null) operationManual.SetActive(false);
                        imagePanelController.gameObject.SetActive(false);
                        videoPanelController.gameObject.SetActive(false);
                    }
                }
                startTime = -1;
            }
            else
            {
                startTime = Time.time;
            }

        }


    }

    public void LoadModuleAndStep(string moduleStepCombinedName)
    {
#if ENABLE_MULTIUSERS
        if (view != null && PhotonNetwork.NetworkClientState == ClientState.Joined && PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("RPC Call ReceiveStepModuleRPC:" + moduleStepCombinedName);
            view.RPC("LoadModuleAndStepNow", RpcTarget.AllBuffered, moduleStepCombinedName);
        }
#else
        LoadModuleAndStepNow(moduleStepCombinedName);
#endif
    }

#if ENABLE_MULTIUSERS
    [PunRPC]
#endif
    public void LoadModuleAndStepNow(string moduleStepCombinedName)
    {
        //Debug.LogError("LoadModuleAndStepNow:" + moduleStepCombinedName);
        RemoveAdditionalPanelsFromScene();

        int moduleIndex = -1;
        int stepIndex = -1;
        int dotIndex = moduleStepCombinedName.IndexOf(".");

        if (dotIndex < 1)
        {
            Debug.LogError("Combined moduleStep name:" + moduleStepCombinedName + " does not have a . to separate the module index from the step index");
            return;
        }

        int.TryParse(moduleStepCombinedName.Substring(0, dotIndex), out moduleIndex);
        int.TryParse(moduleStepCombinedName.Substring(dotIndex + 1), out stepIndex);
        Transform moduleTransform = transform.GetChild(0).GetChild(moduleIndex);

        currentStepIdx = stepIndex - 1;
        currentStep = moduleTransform.GetChild(currentStepIdx).GetComponent<Step>();

        Debug.Log("LoadModuleAndStep: Now at module:" + moduleIndex + " step:" + stepIndex);

        steps = new Step[moduleTransform.childCount];
        for (int i = 0; i < moduleTransform.childCount; i++)
        {
            steps[i] = moduleTransform.GetChild(i).GetComponent<Step>();
            if (steps[i].firstStepInModule)
            {
                firstStepInModuleIdx = i;
            }
        }

        UpdateUI();
    }

    private void UpdateCurrentStep()
    {
        currentStep = steps[currentStepIdx];
    }

    private void HandleStepChange()
    {
        Debug.LogError("Handle step change:" + currentStepIdx);
        RemoveAdditionalPanelsFromScene();
        UpdateCurrentStep();
        UpdateUI();
    }

    private void RemoveAllPanelsFromScene()
    {
        //Reset panels
        modulePanel.SetActive(false);
        RemoveAdditionalPanelsFromScene();
    }

    private void RemoveAdditionalPanelsFromScene()
    {
        //Set panels inactive
        videoPanelController.SetInactive();
        imagePanelController.SetInactive();
        charMenuObjects.SetActive(false);
        stepMenuObjects.SetActive(false);
        warningPanel.SetActive(false);
        secondWarningPanel.SetActive(false);
        additionalInformationPanel.SetActive(false);
        operationManual.SetActive(false);
        if ( partPanel != null ) partPanel.SetActive(false);

        if (currentStep != null)
        {
            currentStep.DisableAnimation();
        }

        // Destroy Part Models
        //for (int i = 0; i < gridObjectCollectionTransform.childCount; i++) Destroy(gridObjectCollectionTransform.GetChild(i).gameObject);
    }

    private void UpdateUI()
    {
        modulePanel.SetActive(true);
        videoPanelController.SetActive();
        recordingButton.SetActive(true); 
        UpdatePanelText();
        UpdateButttonNavigationVisibility();
        LoadAnimationsAndModels();
        LoadImages();
        LoadVideos();
        LoadAudioClips();
        LoadAdditionalInstructions();
        LoadWarnings();
        LoadManualPage();
    }

    private void UpdatePanelText()
    {
        panelText.text = currentStep.GetStepDestription();
        int currentStepNumber = (currentStepIdx - firstStepInModuleIdx) + 1;
        int totalSteps = steps.Length - firstStepInModuleIdx;
        panelTitle.text = "Step " + currentStepNumber.ToString() + "/" + totalSteps.ToString();
    }

    private void UpdateButttonNavigationVisibility()
    {
        // If first or last step, remove back or next buttons, respectively
        if (currentStepIdx == steps.Length - 1)
        {
            nextButton.SetActive(false);
        }
        else
        {
            nextButton.SetActive(true);
        }
        if (currentStep.firstStepInModule)
        {
            backButton.SetActive(false);
        }
        else
        {
            backButton.SetActive(true);
        }
    }

    private void LoadAnimationsAndModels()
    {
        
        animationPanel.transform.localPosition = new Vector3(0f, 0f, 0f);
        animationPanel.transform.localRotation = Quaternion.identity;

        if (currentStep.anim != null)
        {
            // Set animation and panel active
            currentStep.anim.SetActive(true);

            //if ( partPanel != null ) partPanel.SetActive(true);

            // Add models to grid object collection
            /*var models = currentStep.GetModelsToDisplay();
            for (int i = 0; i < models.Count; i++)
            {
                GameObject card = Instantiate(cardPrefab, gridObjectCollectionTransform);
                var cardBehavior = card.GetComponent<ModelCard>();
                cardBehavior.SetText(currentStep.modelsToShow[i]);
                card.transform.localScale = Vector3.one;

                // Duplicate and configure model
                GameObject duplicate = Instantiate(models[i], cardBehavior.modelParent);
                //duplicate.transform.parent = gridObjectCollectionTransform;
                duplicate.transform.localScale = modelScaleFactor;
                duplicate.transform.rotation = models[i].transform.rotation;
                duplicate.AddComponent<Rotate>();
                duplicate.transform.localPosition = new Vector3(0, -0.025f);

            }
            
            StartCoroutine(InvokeUpdateCollection());*/

            //currentStep.PlayAnimation();
        }

        playButton.SetActive(currentStep.anim != null);
    }

    public void ReplayAnimation()
    {
        if (currentStep.anim)
        {
            currentStep.PlayAnimation();
            StartCoroutine(WaitForAnimToCompleteAndReplay());
        }
    }

    IEnumerator WaitForAnimToCompleteAndReplay()
    {
        while (currentStep.anim.activeSelf) yield return new WaitForSeconds(0.1f);

        currentStep.anim.SetActive(true);
        currentStep.PlayAnimation();

        while (currentStep.anim.activeSelf) yield return new WaitForSeconds(0.1f);

        // make sure to re-activate the animation so it does not disappear
        currentStep.anim.SetActive(true);

        yield return null;
    }

    private IEnumerator InvokeUpdateCollection()
    {
        yield return new WaitForEndOfFrame();
        gridObjectCollection.UpdateCollection();
        yield return new WaitForEndOfFrame();
        scrollingObjectCollection.UpdateContent();
    }

    private void LoadImages()
    {
        if (currentStep.HasImages())
        {
            photoButton.SetActive(true);
            imagePanelController.images = currentStep.GetImages();
        }
        else
        {
            photoButton.SetActive(false);
        }
    }

    private void LoadAudioClips()
    {
        if (currentStep.HasAudioClips())
        {
            // anything that need to be turned on when audio clip is present
            if (audioPlayer != null)
            {
                audioPlayer.clip = currentStep.GetAudioClips()[0];
                audioPlayer.Play();
            }
        }
        else
        {
        }
    }

    private void LoadVideos()
    {
        if (currentStep.HasVideos())
        {
            videoPanelController.SetActive();
            videoButton.SetActive(true);
            videoPanelController.SetVideoClip(currentStep.GetVideos()[0]);
        }
        else
        {
            videoPanelController.SetInactive();
            videoButton.SetActive(false);
        }
    }

    void LoadManualPage ()
    {
        if ( currentStep.manualPageNumber != 0 )
            operationManual.GetComponentInChildren<Paroxe.PdfRenderer.Examples.PDFDocumentRenderToTextureExample>().GoToPage(currentStep.manualPageNumber);
    }


    private void LoadAdditionalInstructions()
    {
        if (!currentStep.additionalInformation.Equals(""))
        {
            additionalInformationPanel.SetActive(true);
            additionalInformationText.text = currentStep.additionalInformation;
        }
    }

    private void LoadWarnings()
    {
        // If there is a warning to display, show it
        if (currentStep.ShouldDisplayWarning())
        {
            warningPanel.SetActive(true);
            warningText.text = currentStep.GetWarningText();
            secondWarningPanel.SetActive(true);
            secondWarningText.text = currentStep.GetWarningText();
        }
    }


    /*
     * ------- CONFIRMATION DIALOGS ------------
     */

    private void DisplayLoadModuleConfirmationDialog()
    {

        int moduleIndex = -1;
        int dotIndex = moduleStepCombinedNameToLoad.IndexOf(".");
        int.TryParse(moduleStepCombinedNameToLoad.Substring(0, dotIndex), out moduleIndex);

        string moduleName = GetModuleName(moduleIndex);

        LoadModuleAndStep(moduleStepCombinedNameToLoad);
        /**Dialog confirmationDialog = Dialog.Open(DialogPrefabSmall, DialogButtonType.Yes | DialogButtonType.No, null, "Would you like to load the" + moduleName + "module?", true);
        if (confirmationDialog != null || true)
        {
            RemoveAllPanelsFromScene();
            confirmationDialog.OnClosed += OnClosedLoadModuleDialog;
            displayingConfirmationDialog = true;
        }**/
    }

    private void OnClosedLoadModuleDialog(DialogResult result)
    {
        displayingConfirmationDialog = false;

        if (result.Result == DialogButtonType.Yes)
        {
            // Load module
            LoadModuleAndStep(moduleStepCombinedNameToLoad);
        }
    }

    private void ShowConfirmationDialog()
    {
        Dialog confirmationDialog = Dialog.Open(DialogPrefabSmall, DialogButtonType.Yes | DialogButtonType.No, null, currentStep.GetConfirmationText(), true);
        if (confirmationDialog != null)
        {
            RemoveAllPanelsFromScene();
            confirmationDialog.OnClosed += OnClosedDialogEvent;
            displayingConfirmationDialog = true;
        }
    }

    private void OnClosedDialogEvent(DialogResult result)
    {
        displayingConfirmationDialog = false;
        LoadModuleAndStep(result.Result == DialogButtonType.Yes ? currentStep.moduleStepYesConfirmation : currentStep.moduleStepNoConfirmation);
    }

    /*
     * ------- MODULE PANEL BUTTONS --------
     */
    public void Home()
    {
#if ENABLE_MULTIUSERS
        if (view != null && PhotonNetwork.NetworkClientState == ClientState.Joined && PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("RPC Call HomeNow:");
            view.RPC("HomeNow", RpcTarget.AllBuffered);
        }
#else
        HomeNow();
#endif
    }

#if ENABLE_MULTIUSERS
[PunRPC]
#endif
    public void HomeNow()
    {
        ResetStepIdx();
        RemoveAllPanelsFromScene();
        ShowMainMenu();
    }

    public void GoToPreviousStep()
    {
#if ENABLE_MULTIUSERS
        if (view != null && PhotonNetwork.NetworkClientState == ClientState.Joined && PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("RPC Call GoToPreviousStepNow:");
            view.RPC("GoToPreviousStepNow", RpcTarget.AllBuffered);
        }
#else
        GoToPreviousStepNow();
#endif
    }

#if ENABLE_MULTIUSERS
[PunRPC]
#endif
    public void GoToPreviousStepNow()
    {
        if (currentStepIdx != 0 && !displayingConfirmationDialog)
        {
            currentStepIdx = Mathf.Clamp(currentStepIdx - 1, 0, steps.Length);
            HandleStepChange();
        }
    }

    public void GoToNextStep()
    {
#if ENABLE_MULTIUSERS
        if (view != null && PhotonNetwork.NetworkClientState == ClientState.Joined && PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("RPC Call GoToNextStepNow:");
            view.RPC("GoToNextStepNow", RpcTarget.AllBuffered);
        }
#else
        GoToNextStepNow();
#endif
    }

#if ENABLE_MULTIUSERS
[PunRPC]
#endif
    public void GoToNextStepNow()
    {
        Debug.LogError("GoToNextStepNow");

        if (currentStepIdx != steps.Length - 1 && !displayingConfirmationDialog)
        {
#if !(UNITY_ANDROID || UNITY_IOS)
            if (currentStep.ShouldAskForConfirmation())
            {
                ShowConfirmationDialog();
                return;
            }
#endif
            currentStepIdx = Mathf.Clamp(currentStepIdx + 1, 0, steps.Length);
            HandleStepChange();
        }
    }
    
    public void OnOperationsManualPressed()
    {
        if (operationManual != null) operationManual.SetActive(true);
        //videoPanelController.gameObject.SetActive(false);
        //imagePanelController.gameObject.SetActive(false);
    }

    public void Call()
    {

    }

    public void Ask()
    {

    }

    // Helpers
    private void ShowMainMenu()
    {
        mainMenuObjects.SetActive(true);
    }

    private void HideMainMenu()
    {
        mainMenuObjects.SetActive(false);
    }

    public void LogStep(int moduleId, int stepId)
    {
        if (stepId != 0) stepId++;

        if (dataLogger && dataLogger.isActiveAndEnabled)
        {        
            dataLogger.LogStep(moduleId.ToString(), stepId.ToString());
        }
    }

    private string GetModuleName(int moduleNumber)
    {
        currentModuleIdx = moduleNumber;
        string res = " Char " + moduleNumber.ToString() + " ";

        return res;
    }


    public void ResetStepIdx()
    {
        currentModuleIdx = 0;
        currentStepIdx = 0;
    }


    public void ToggleRecording()
    {
        animationPanel.SetActive(recording);
        recording = !recording;
        if (recording)
        {
            audioPlayer.clip = startAudio;
            audioPlayer.Play();
        }
        else
        {
            audioPlayer.clip = stopAudio;
            audioPlayer.Play();
        }
    }

    public void CancelRecording()
    {
        cancel = true;
        cancelCnt = 10; 
        Debug.Log("Cancel!");
    }


    public int getStepIdx()
    {
        // Debug.Log("Step = " + currentStepIdx);
        if (cancel)
        {
            cancelCnt--;
            if (cancelCnt == 0)
            {
                cancel = false;
                Debug.Log("Cancel!!");
                return 0;
            }

            return -1; 
        }
        else if (!recording)
            return 0;
        else
            return currentStepIdx + 1;
    }

}
