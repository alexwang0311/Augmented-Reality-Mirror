/* Created by: Alex Wang
 * Date: 07/27/2019
 * MySkeletonRenderer is responsible for creating and rendering the joints and the bones.
 * It only renders the skeleton of the body that was detected by Orbbec the first.
 * It is adapted from the original SkeletonRenderer from the Astra Orbbec SDK 2.0.16.
 */
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class MySkeletonRenderer : MonoBehaviour
{
    private long _lastFrameIndex = -1;

    private Astra.Body[] _bodies;
    private GameObject[] _bodyJoints;
    private GameObject[] _bodyParts;

    private readonly Vector3 NormalPoseScale = new Vector3(0.1f, 0.1f, 0.1f);
    private readonly Vector3 GripPoseScale = new Vector3(0.2f, 0.2f, 0.2f);

    public GameObject JointPrefab;
    public Transform JointRoot;

    #region 3d body model prefabs
    //Bone Prefabs
    public GameObject Prefab_Head_Neck;
    public GameObject Prefab_MidSpine_ShoulderSpine;
    public GameObject Prefab_BaseSpine_MidSpine;
    public GameObject Prefab_LeftShoulder_LeftElbow;
    public GameObject Prefab_LeftElbow_LeftWrist;
    public GameObject Prefab_ShoudlerSpine_LeftShoulder;
    public GameObject Prefab_ShoulderSpine_RightShoulder;
    public GameObject Prefab_RightShoulder_RightElbow;
    public GameObject Prefab_RightElbow_RightWrist;
    public GameObject Prefab_ShoulderSpine_Neck;
    public GameObject Prefab_BaseSpine_LeftHip;
    public GameObject Prefab_LeftHip_LeftKnee;
    public GameObject Prefab_LeftKnee_LeftFoot;
    public GameObject Prefab_BaseSpine_RightHip;
    public GameObject Prefab_RightHip_RightKnee;
    public GameObject Prefab_RightKnee_RightFoot;
    public GameObject Prefab_Head_bone;
    public GameObject Prefab_LeftHand;
    public GameObject Prefab_RightHand;

    private readonly float TestBoneThickness = 1f;
    private readonly float HeadBoneThickness = 0.2f;

    #endregion

    #region Bone Data Structure
    /// <summary>
    /// Bone is connector of two joints
    /// </summary>
    private struct Bone
    {
        public Astra.JointType _startJoint;
        public Astra.JointType _endJoint;

        public Bone(Astra.JointType startJoint, Astra.JointType endJoint)
        {
            _startJoint = startJoint;
            _endJoint = endJoint;
        }
    };

    /// <summary>
    /// Skeleton structure = list of bones = list of joint connectors
    /// </summary>
    private Bone[] Bones = new Bone[]
    {
            // spine, neck, and head
            new Bone(Astra.JointType.BaseSpine, Astra.JointType.MidSpine),
            new Bone(Astra.JointType.MidSpine, Astra.JointType.ShoulderSpine),
            new Bone(Astra.JointType.ShoulderSpine, Astra.JointType.Neck),
            new Bone(Astra.JointType.Neck, Astra.JointType.Head),
            // left arm
            new Bone(Astra.JointType.ShoulderSpine, Astra.JointType.LeftShoulder),
            new Bone(Astra.JointType.LeftShoulder, Astra.JointType.LeftElbow),
            new Bone(Astra.JointType.LeftElbow, Astra.JointType.LeftWrist),
            new Bone(Astra.JointType.LeftWrist, Astra.JointType.LeftHand),
            // right arm
            new Bone(Astra.JointType.ShoulderSpine, Astra.JointType.RightShoulder),
            new Bone(Astra.JointType.RightShoulder, Astra.JointType.RightElbow),
            new Bone(Astra.JointType.RightElbow, Astra.JointType.RightWrist),
            new Bone(Astra.JointType.RightWrist, Astra.JointType.RightHand),
            // left leg
            new Bone(Astra.JointType.BaseSpine, Astra.JointType.LeftHip),
            new Bone(Astra.JointType.LeftHip, Astra.JointType.LeftKnee),
            new Bone(Astra.JointType.LeftKnee, Astra.JointType.LeftFoot),
            // right leg
            new Bone(Astra.JointType.BaseSpine, Astra.JointType.RightHip),
            new Bone(Astra.JointType.RightHip, Astra.JointType.RightKnee),
            new Bone(Astra.JointType.RightKnee, Astra.JointType.RightFoot),
    };
    #endregion

    #region Selection joints

    /* Selection joints
     * Current Design: right and left hands as selection joints
     * Switch between modes (Bone / Muscle / Joint)
     */
    private Astra.JointType selectionJointTypeA = Astra.JointType.LeftHand;     //use which joint(s) type as your selectionJoint
    private Astra.JointType selectionJointTypeB = Astra.JointType.RightHand;
    public static string selectedBoneName = "";

    private float baseSpineX = 0, leftHipX = 0, shoulderSpineY = 0, neckY = 0;
    private float horizontalError = 0, verticalError = 0;

    private readonly float SelectBoneMultiplyFactor = 3f;
    #endregion

    #region Helper Methods
    private Astra.Body GetFirstBody(Astra.Body[] bodies)
    {
        if (bodies.Length == 0)
        {
            return null;
        }

        var body = bodies[0];
        if (body.Id == 0)
        {
            return null;
        }
        return body;
    }

    private static int FindJointIndex(Astra.Body body, Astra.JointType jointType)
    {
        for (int i = 0; i < body.Joints.Length; i++)
        {
            if (body.Joints[i].Type == jointType)
            {
                return i;
            }
        }
        return -1;
    }

    private float find2DAngles(float x, float y)
    {
        return -RadiansToDegrees((float)Mathf.Atan2(x, y));
    }

    private float RadiansToDegrees(float radians)
    {
        float angle = radians * 180 / (float)Mathf.PI;
        return angle;
    }

    private bool withinRange(Vector3 posJ, Vector3 posA, Vector3 posB)
    {
        if (withinPoints(posJ.x, posA.x, posB.x, horizontalError) && withinPoints(posJ.y, posA.y, posB.y, verticalError))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool withinPoints(float x, float a, float b, float error = 0)
    {
        float tmp;
        if (a > b)
        {
            tmp = a; a = b; b = tmp;
        }
        return x >= a - error && x <= b + error;
    }

    private string GetJointName(Astra.Joint joint)
    {
        return (joint.ToString().Split(' ')[1]).Split(',')[0];
    }
    #endregion

    void Start()
    {
        _bodyJoints = new GameObject[19];
        for (int i = 0; i < _bodyJoints.Length; ++i)
        {
            _bodyJoints[i] = (GameObject)Instantiate(JointPrefab, Vector3.zero, Quaternion.identity);
            _bodyJoints[i].transform.SetParent(JointRoot);
        }
        _bodyParts = new GameObject[Bones.Length];

        _bodyParts[0] = (GameObject)Instantiate(Prefab_BaseSpine_MidSpine, Vector3.zero, Quaternion.identity);
        _bodyParts[1] = (GameObject)Instantiate(Prefab_MidSpine_ShoulderSpine, Vector3.zero, Quaternion.identity);
        _bodyParts[2] = (GameObject)Instantiate(Prefab_ShoulderSpine_Neck, Vector3.zero, Quaternion.identity);
        _bodyParts[3] = (GameObject)Instantiate(Prefab_Head_bone, Vector3.zero, Quaternion.identity);
        _bodyParts[4] = (GameObject)Instantiate(Prefab_ShoudlerSpine_LeftShoulder, Vector3.zero, Quaternion.identity);
        _bodyParts[5] = (GameObject)Instantiate(Prefab_LeftShoulder_LeftElbow, Vector3.zero, Quaternion.identity);
        _bodyParts[6] = (GameObject)Instantiate(Prefab_LeftElbow_LeftWrist, Vector3.zero, Quaternion.identity);
        _bodyParts[7] = (GameObject)Instantiate(Prefab_LeftHand, Vector3.zero, Quaternion.identity);
        _bodyParts[8] = (GameObject)Instantiate(Prefab_ShoulderSpine_RightShoulder, Vector3.zero, Quaternion.identity);
        _bodyParts[9] = (GameObject)Instantiate(Prefab_RightShoulder_RightElbow, Vector3.zero, Quaternion.identity);
        _bodyParts[10] = (GameObject)Instantiate(Prefab_RightElbow_RightWrist, Vector3.zero, Quaternion.identity);
        _bodyParts[11] = (GameObject)Instantiate(Prefab_RightHand, Vector3.zero, Quaternion.identity);
        _bodyParts[12] = (GameObject)Instantiate(Prefab_BaseSpine_LeftHip, Vector3.zero, Quaternion.identity);
        _bodyParts[13] = (GameObject)Instantiate(Prefab_LeftHip_LeftKnee, Vector3.zero, Quaternion.identity);
        _bodyParts[14] = (GameObject)Instantiate(Prefab_LeftKnee_LeftFoot, Vector3.zero, Quaternion.identity);
        _bodyParts[15] = (GameObject)Instantiate(Prefab_BaseSpine_RightHip, Vector3.zero, Quaternion.identity);
        _bodyParts[16] = (GameObject)Instantiate(Prefab_RightHip_RightKnee, Vector3.zero, Quaternion.identity);
        _bodyParts[17] = (GameObject)Instantiate(Prefab_RightKnee_RightFoot, Vector3.zero, Quaternion.identity);

        _bodies = new Astra.Body[Astra.BodyFrame.MaxBodies];
    }

    public void OnNewFrame(Astra.BodyStream bodyStream, Astra.BodyFrame frame)
    {
        if (frame.Width == 0 ||
            frame.Height == 0)
        {
            return;
        }

        if (_lastFrameIndex == frame.FrameIndex)
        {
            return;
        }

        _lastFrameIndex = frame.FrameIndex;

        frame.CopyBodyData(ref _bodies);
        UpdateSkeletonsFromBodies(_bodies);
    }


    void UpdateSkeletonsFromBodies(Astra.Body[] bodies)
    {
        var body = GetFirstBody(bodies);
        if (body != null) {
            //Render the joints
            for (int i = 0; i < body.Joints.Length; i++)
            {
                var skeletonJoint = _bodyJoints[i];
                var bodyJoint = body.Joints[i];

                if (bodyJoint.Status != Astra.JointStatus.NotTracked)
                {
                    if (!skeletonJoint.activeSelf)
                    {
                        skeletonJoint.SetActive(true);
                    }


                    skeletonJoint.transform.localPosition =
                        new Vector3(bodyJoint.WorldPosition.X / 1000f,
                                    bodyJoint.WorldPosition.Y / 1000f,
                                    bodyJoint.WorldPosition.Z / 1000f);


                    //skeletonJoint.Orient.Matrix:
                    // 0, 			1,	 		2,
                    // 3, 			4, 			5,
                    // 6, 			7, 			8
                    // -------
                    // right(X),	up(Y), 		forward(Z)

                    //Vector3 jointRight = new Vector3(
                    //    bodyJoint.Orientation.M00,
                    //    bodyJoint.Orientation.M10,
                    //    bodyJoint.Orientation.M20);

                    Vector3 jointUp = new Vector3(
                        bodyJoint.Orientation.M01,
                        bodyJoint.Orientation.M11,
                        bodyJoint.Orientation.M21);

                    Vector3 jointForward = new Vector3(
                        bodyJoint.Orientation.M02,
                        bodyJoint.Orientation.M12,
                        bodyJoint.Orientation.M22);

                    skeletonJoint.transform.rotation =
                        Quaternion.LookRotation(jointForward, jointUp);

                    skeletonJoint.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                }
                else
                {
                    if (skeletonJoint.activeSelf) skeletonJoint.SetActive(false);
                }
            }

            #region calculate errors
            // calculate the horizontal/vertical Error for selection
            if (baseSpineX != 0 && leftHipX != 0)
            {
                horizontalError = 0.5f * Mathf.Abs(baseSpineX - leftHipX);
            }
            if (neckY != 0 && shoulderSpineY != 0)
            {
                verticalError = 0.5f * Mathf.Abs(neckY - shoulderSpineY);
            }

            #endregion

            //Render the bones
            for (int i = 0; i < Bones.Length; i++)
            {
                //actual gameobject bones
                var skeletonBone = _bodyParts[i];
                //bones a body should have
                var bodyBone = Bones[i];
                int startIndex = FindJointIndex(body, bodyBone._startJoint);
                int endIndex = FindJointIndex(body, bodyBone._endJoint);
                var startJoint = body.Joints[startIndex];
                var endJoint = body.Joints[endIndex];

                if (startJoint.Status != Astra.JointStatus.NotTracked && endJoint.Status != Astra.JointStatus.NotTracked)
                {
                    if (!skeletonBone.activeSelf)
                    {
                        skeletonBone.SetActive(true);
                    }


                    #region Draw all bones
                    Vector3 startPosition = _bodyJoints[startIndex].transform.position;
                    Vector3 endPosition = _bodyJoints[endIndex].transform.position;

                    float squaredMagnitude = Mathf.Pow(endPosition.x - startPosition.x, 2) + Mathf.Pow(endPosition.y - startPosition.y, 2);
                    float magnitude = Mathf.Sqrt(squaredMagnitude);

                    skeletonBone.transform.position = (startPosition + endPosition) / 2.0f;
                    skeletonBone.transform.localEulerAngles = new Vector3(0, 0, find2DAngles(endPosition.x - startPosition.x, endPosition.y - startPosition.y));

                    //Scale the head
                    if (startJoint.Type == Astra.JointType.Neck)
                    {
                        skeletonBone.transform.localScale = new Vector3(HeadBoneThickness, magnitude * 1.3f, HeadBoneThickness);
                    }
                    else if (startJoint.Type == Astra.JointType.LeftWrist || startJoint.Type == Astra.JointType.RightWrist)
                    {
                        skeletonBone.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                    }
                    //Scale other bones
                    else
                    {
                        skeletonBone.transform.localScale = new Vector3(TestBoneThickness, magnitude, TestBoneThickness);
                    }
                    #endregion

                    #region Update selected bones
                    //Rescale the selected bones
                    if ((selectionJointTypeA != startJoint.Type && selectionJointTypeA != endJoint.Type)
                         && (withinRange(_bodyJoints[FindJointIndex(body, selectionJointTypeA)].transform.position, startPosition, endPosition)))
                    {

                        skeletonBone.transform.localScale = new Vector3(skeletonBone.transform.localScale.x * SelectBoneMultiplyFactor,
                                                                        magnitude * SelectBoneMultiplyFactor,
                                                                        skeletonBone.transform.localScale.z * SelectBoneMultiplyFactor);
                        selectedBoneName = GetJointName(startJoint) + "_" + GetJointName(endJoint);
                    }

                    if ((selectionJointTypeB != startJoint.Type && selectionJointTypeB != endJoint.Type)
                         && (withinRange(_bodyJoints[FindJointIndex(body, selectionJointTypeB)].transform.position, startPosition, endPosition)))
                    {
                        skeletonBone.transform.localScale = new Vector3(skeletonBone.transform.localScale.x * SelectBoneMultiplyFactor,
                                                                        magnitude * SelectBoneMultiplyFactor,
                                                                        skeletonBone.transform.localScale.z * SelectBoneMultiplyFactor);
                        selectedBoneName = GetJointName(startJoint) + "_" + GetJointName(endJoint);
                    }
                    #endregion
                }
                else
                {
                    if (skeletonBone.activeSelf) skeletonBone.SetActive(false);
                }

               }
            }
        }
    }
