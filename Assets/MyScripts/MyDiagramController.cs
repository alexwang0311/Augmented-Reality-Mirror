using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyDiagramController : MonoBehaviour {
    

    public GameObject Left_Forearm;
    public GameObject Right_Forearm;
    public GameObject ShoulderSpine_MidSpine;
    public GameObject MidSpine_BaseSpine;
    public GameObject Left_Hip;
    public GameObject Right_Hip;
    public GameObject Left_UpperLeg;
    public GameObject Right_UpperLeg;
    public GameObject Left_LowerLeg;
    public GameObject Right_LowerLeg;
    public GameObject Right_Upperarm;
    public GameObject Left_Upperarm;

	
	//Update is called once per frame
	void Update () {
        updateBoneDiagrams();
	}

    private void updateBoneDiagrams()
    {
        switch (MySkeletonRenderer.selectedBoneName)
        {
            case "LeftShoulder_LeftElbow":
                display(Left_Upperarm);
                break;
            case "RightShoulder_RightElbow":
                display(Right_Upperarm);
                break;
            case "LeftElbow_LeftWrist":
                display(Left_Forearm);
                break;
            case "RightElbow_RightWrist":
                display(Right_Forearm);
                break;
            case "MidSpine_ShoulderSpine":
                display(ShoulderSpine_MidSpine);
                break;
            case "BaseSpine_MidSpine":
                display(MidSpine_BaseSpine);
                break;
            case "BaseSpine_LeftHip":
                display(Left_Hip);
                break;
            case "BaseSpine_RightHip":
                display(Right_Hip);
                break;
            case "LeftHip_LeftKnee":
                display(Left_UpperLeg);
                break;
            case "LeftKnee_LeftFoot":
                display(Left_LowerLeg);
                break;
            case "RightHip_RightKnee":
                display(Right_UpperLeg);
                break;
            case "RightKnee_RightFoot":
                display(Right_LowerLeg);
                break;
            default:
                display(null);
                break;
        }
    }

    private void display(GameObject gameObject)
    {
        //Deactivate all other diagrams
        hideAllBones();

        if (gameObject != null)
        {
            gameObject.SetActive(true);
            //Debug.Log(gameObject.name);
        }
    }

    private void hideAllBones()
    {
        Right_Upperarm.SetActive(false);
        Left_Upperarm.SetActive(false);
        Left_Forearm.SetActive(false);
        Right_Forearm.SetActive(false);
        ShoulderSpine_MidSpine.SetActive(false);
        MidSpine_BaseSpine.SetActive(false);
        Left_Hip.SetActive(false);
        Right_Hip.SetActive(false);
        Left_UpperLeg.SetActive(false);
        Right_UpperLeg.SetActive(false);
        Left_LowerLeg.SetActive(false);
        Right_LowerLeg.SetActive(false);
    }
}
