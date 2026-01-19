using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.Events;
using System.Collections.Generic;

public enum HandGesture
{
    None = 0,
    Fisting = 1,
    FacingDown = 2,
    FacingSelf = 3
}

public class HandGestureManager : MonoBehaviour
{
    private Dictionary<Handedness, HandGesture> m_HandGestures;

    private const float FINGER_MAX_DIST = 0.15f;

    private const float FINGER_MIN_ANGLE = 40f;

    public UnityEvent<Handedness, HandGesture, HandGesture> OnHandGestureChanged;

    private void Start()
    {
        m_HandGestures = new();
        m_HandGestures.Add(Handedness.Left, HandGesture.None);
        m_HandGestures.Add(Handedness.Right, HandGesture.None);
    }

    public void OnTrackingAcquired(Handedness handedness)
    {
        
    }

    public void OnTrackingLost(Handedness handedness)
    {
        if (m_HandGestures[handedness] != HandGesture.None)
        {
            var prevGesture = m_HandGestures[handedness];
            m_HandGestures[handedness] = HandGesture.None;
            OnHandGestureChanged?.Invoke(handedness, prevGesture, HandGesture.None);
        }
    }

    public void OnUpdatedHand(Handedness handedness, Hand hand)
    {
        if (IsFisting(hand))
        {
            OnHandGestureValidated(handedness, HandGesture.Fisting);
            return;
        }

        //if (IsFacingUp(hand))
        //{
        //    OnHandGestureValidated(handedness, HandGesture.FacingDown);
        //    return;
        //}

        if (IsFacingSelf(hand))
        {
            OnHandGestureValidated(handedness, HandGesture.FacingSelf);
            return;
        }

        OnHandGestureValidated(handedness, HandGesture.None);
    }

    private void OnHandGestureValidated(Handedness handedness, HandGesture handGesture)
    {
        if (m_HandGestures[handedness] != handGesture)
        {
            var prevGesture = m_HandGestures[handedness];
            m_HandGestures[handedness] = handGesture;
            OnHandGestureChanged?.Invoke(handedness, prevGesture, handGesture);
        }
    }

    private bool IsFisting(Hand hand)
    {
        var wrist = hand.GetHandJointPose(XRHandJointID.Wrist);
        var thumbTip = hand.GetHandJointPose(XRHandJointID.ThumbTip);
        var indexTip = hand.GetHandJointPose(XRHandJointID.IndexTip);
        var middleTip = hand.GetHandJointPose(XRHandJointID.MiddleTip);
        var ringTip = hand.GetHandJointPose(XRHandJointID.RingTip);
        var littleTip = hand.GetHandJointPose(XRHandJointID.LittleTip);

        var thumbDist = Vector3.Distance(wrist.position, thumbTip.position);
        var indexDist = Vector3.Distance(wrist.position, indexTip.position);
        var middleDist = Vector3.Distance(wrist.position, middleTip.position);
        var ringDist = Vector3.Distance(wrist.position, ringTip.position);
        var littleDist = Vector3.Distance(wrist.position, littleTip.position);

        return thumbDist < FINGER_MAX_DIST && indexDist < FINGER_MAX_DIST && middleDist < FINGER_MAX_DIST && ringDist < FINGER_MAX_DIST && littleDist < FINGER_MAX_DIST;
    }

    private bool IsFacingUp(Hand hand)
    {
        var indexMetacarpal = hand.GetHandJointPose(XRHandJointID.IndexMetacarpal);
        var middleMetacarpal = hand.GetHandJointPose(XRHandJointID.MiddleMetacarpal);
        var ringMetacarpal = hand.GetHandJointPose(XRHandJointID.RingMetacarpal);
        var littleMetacarpal = hand.GetHandJointPose(XRHandJointID.LittleMetacarpal);

        var indexAngle = Vector3.Angle(-indexMetacarpal.up, Vector3.down);
        var middleAngle = Vector3.Angle(-middleMetacarpal.up, Vector3.down);
        var ringAngle = Vector3.Angle(-ringMetacarpal.up, Vector3.down);
        var littleAngle = Vector3.Angle(-littleMetacarpal.up, Vector3.down);

        return indexAngle < FINGER_MIN_ANGLE && middleAngle < FINGER_MIN_ANGLE && ringAngle < FINGER_MIN_ANGLE && littleAngle < FINGER_MIN_ANGLE;
    }

    private bool IsFacingSelf(Hand hand)
    {
        var indexProximal = hand.GetHandJointPose(XRHandJointID.IndexProximal);
        var middleProximal = hand.GetHandJointPose(XRHandJointID.MiddleProximal);
        var ringProximal = hand.GetHandJointPose(XRHandJointID.RingProximal);
        var littleProximal = hand.GetHandJointPose(XRHandJointID.LittleProximal);

        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        cameraForward = cameraForward.normalized;

        var indexAngle = Vector3.Angle(-indexProximal.up, -cameraForward);
        var middleAngle = Vector3.Angle(-middleProximal.up, -cameraForward);
        var ringAngle = Vector3.Angle(-ringProximal.up, -cameraForward);
        var littleAngle = Vector3.Angle(-littleProximal.up, -cameraForward);

        return indexAngle < FINGER_MIN_ANGLE && middleAngle < FINGER_MIN_ANGLE && ringAngle < FINGER_MIN_ANGLE && littleAngle < FINGER_MIN_ANGLE;
    }
}
