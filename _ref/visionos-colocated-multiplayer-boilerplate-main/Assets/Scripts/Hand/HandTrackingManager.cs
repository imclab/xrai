using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.Events;

public class HandTrackingManager : MonoBehaviour
{
    [SerializeField] private Hand m_LeftHand;

    [SerializeField] private Hand m_RightHand;

    [SerializeField] private bool m_HandJointsVisibility = true;

    // True if the left hand is currently detected
    public bool HasLeftHand => m_LeftHand.gameObject.activeSelf;

    // True if the right hand is currently detected
    public bool HasRightHand => m_RightHand.gameObject.activeSelf;

    public Hand LeftHand => m_LeftHand;

    public Hand RightHand => m_RightHand;

    private XRHandSubsystem m_HandSubsystem;

    // Invoked when hand is detected
    public UnityEvent<Handedness> OnTrackingAcquired;

    // Invoked when tracking is lost
    public UnityEvent<Handedness> OnTrackingLost;

    public UnityEvent<Handedness, Hand> OnUpdatedHand;

    private void Start()
    {
        m_LeftHand.gameObject.SetActive(false);
        m_RightHand.gameObject.SetActive(false);

        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var meshRenderer in meshRenderers)
            meshRenderer.enabled = m_HandJointsVisibility;
    }

    private void Update()
    {
        if (m_HandSubsystem != null && !m_HandSubsystem.running)
        {
            UnsubscribeHandSubsystem();
            return;
        }

        if (m_HandSubsystem == null)
        {
            List<XRHandSubsystem> subsystems = new();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var subsystem in subsystems)
            {
                if (subsystem.running)
                {
                    m_HandSubsystem = subsystem;
                    break;
                }
            }

            if (m_HandSubsystem != null)
            {
                SubscribeHandSubsystem();
            }
        }
    }

    private void SubscribeHandSubsystem()
    {
        if (m_HandSubsystem == null)
            return;

        m_HandSubsystem.trackingAcquired += OnTrackingAcquiredInternal;
        m_HandSubsystem.trackingLost += OnTrackingLostInternal;
        m_HandSubsystem.updatedHands += OnUpdatedHandsInternal;
    }

    private void UnsubscribeHandSubsystem()
    {
        if (m_HandSubsystem == null)
            return;

        m_HandSubsystem.trackingAcquired -= OnTrackingAcquiredInternal;
        m_HandSubsystem.trackingLost -= OnTrackingLostInternal;
        m_HandSubsystem.updatedHands -= OnUpdatedHandsInternal;
    }

    private void OnTrackingAcquiredInternal(XRHand hand)
    {
        if (hand.handedness == Handedness.Left)
            m_LeftHand.gameObject.SetActive(true);
        else if (hand.handedness == Handedness.Right)
            m_RightHand.gameObject.SetActive(true);

        OnTrackingAcquired?.Invoke(hand.handedness);
    }

    private void OnTrackingLostInternal(XRHand hand)
    {
        if (hand.handedness == Handedness.Left)
            m_LeftHand.gameObject.SetActive(false);
        else if (hand.handedness == Handedness.Right)
            m_RightHand.gameObject.SetActive(false);

        OnTrackingLost?.Invoke(hand.handedness);
    }

    private void OnUpdatedHandsInternal(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
    {
        if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != 0)
        {
            for (int i = (int)XRHandJointID.Wrist; i < (int)XRHandJointID.EndMarker; i++)
            {
                XRHandJointID handJointID = (XRHandJointID)i;
                XRHandJoint handJoint = subsystem.leftHand.GetJoint(handJointID);
                if (handJoint.TryGetPose(out var handJointPose))
                {
                    m_LeftHand.SetHandJointPose(handJointID, handJointPose);
                }
            }

            OnUpdatedHand?.Invoke(Handedness.Left, m_LeftHand);
        }

        if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != 0)
        {
            for (int i = (int)XRHandJointID.Wrist; i < (int)XRHandJointID.EndMarker; i++)
            {
                XRHandJointID handJointID = (XRHandJointID)i;
                XRHandJoint handJoint = subsystem.rightHand.GetJoint(handJointID);
                if (handJoint.TryGetPose(out var handJointPose))
                {
                    m_RightHand.SetHandJointPose(handJointID, handJointPose);
                }
            }

            OnUpdatedHand?.Invoke(Handedness.Right, m_RightHand);
        }
    }
}
