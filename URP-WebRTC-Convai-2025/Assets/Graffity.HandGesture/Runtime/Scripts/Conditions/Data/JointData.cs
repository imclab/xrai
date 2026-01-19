using System;
using UnityEngine;
using UnityEngine.XR.Hands;


namespace Graffity.HandGesture.Conditions.Data
{


    /// <summary>
    /// Data of one joint
    /// </summary>
    [Serializable]
    public sealed class JointData
    {


        [field: SerializeField, Tooltip("Type of hand")]
        public HandInfo.HandType Hand { get; private set; } = default;
        [field: SerializeField, Tooltip("Type of joint")]
        public XRHandJointID JointID { get; private set; } = default;


        public bool TryGetJointInfo(HandInfo leftHand, HandInfo rightHand, out JointInfo jointInfo)
        {
            var handInfo = Hand == HandInfo.HandType.Left ? leftHand : rightHand;
            return handInfo.TryGetJointInfo(JointID, out jointInfo);
        }


    }


}