using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;

public class HandMenuController : MonoBehaviour
{
    public SolverHandler handMenuSolverHandler;
    public GameObject submoduleParent;

    public GameObject[] submodules;

    private Vector3 submoduleLeftHandPosition;
    private Vector3 submoduleRightHandPosition;

    private void Start()
    {
        // Store references to submodule menus
        //submodules = GameObject.FindGameObjectsWithTag("SubmoduleHandMenu");
        int numSub = submoduleParent.transform.childCount;
        submodules = new GameObject[numSub];
        for (int i = 0; i < numSub; i++) submodules[i] = submoduleParent.transform.GetChild(i).gameObject;

        // Set module position variables
        submoduleLeftHandPosition = submoduleParent.transform.localPosition;
        submoduleRightHandPosition = new Vector3(-submoduleLeftHandPosition.x, submoduleLeftHandPosition.y, submoduleLeftHandPosition.z);

        ClearSubmoduleOptions();
    }

    public void ClearSubmoduleOptions()
    {
        if (submodules != null) foreach (GameObject o in submodules)
        {
            o.SetActive(false);
        }
    }

    private void UpdateSubmodulePosition()
    {
        // Set menu position according to hand
        if (handMenuSolverHandler.CurrentTrackedHandedness == Microsoft.MixedReality.Toolkit.Utilities.Handedness.Right)
        {
            submoduleParent.transform.localPosition = submoduleRightHandPosition;
        }
        else if (handMenuSolverHandler.CurrentTrackedHandedness == Microsoft.MixedReality.Toolkit.Utilities.Handedness.Left)
        {
            submoduleParent.transform.localPosition = submoduleLeftHandPosition;
        }
    }

    public void OpenSubmoduleHandMenu(GameObject submodule)
    {
        ClearSubmoduleOptions();
        UpdateSubmodulePosition();
        submodule.SetActive(true);
    }
}
