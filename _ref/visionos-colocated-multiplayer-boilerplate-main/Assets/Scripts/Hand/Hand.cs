using UnityEngine;
using UnityEngine.XR.Hands;

public class Hand : MonoBehaviour
{
    [SerializeField] private Handedness m_Handedness;

    [SerializeField] private Transform[] m_HandJoints;

    public Handedness Handedness => m_Handedness;

    public Pose GetHandJointPose(XRHandJointID handJointID)
    {
        int handJointIndex = HandJointID2Index(handJointID);
        Transform handJoint = m_HandJoints[handJointIndex];
        return new Pose(handJoint.position, handJoint.rotation);
    }

    public Transform GetHandJointTransform(XRHandJointID handJointID)
    {
        int handJointIndex = HandJointID2Index(handJointID);
        return m_HandJoints[handJointIndex];
    }

    public void SetHandJointPose(XRHandJointID handJointID, Pose pose)
    {
        int handJointIndex = HandJointID2Index(handJointID);
        m_HandJoints[handJointIndex].SetPositionAndRotation(pose.position, pose.rotation);
    }

    private int HandJointID2Index(XRHandJointID handJointID)
    {
        return handJointID == XRHandJointID.Wrist ? 0 : ((int)handJointID - 1);
    }
}
