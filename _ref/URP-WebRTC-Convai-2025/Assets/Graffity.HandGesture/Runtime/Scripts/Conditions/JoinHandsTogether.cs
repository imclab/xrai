using System;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings required to determine if the user join hands with one's own
    /// </summary>
    [Serializable]
    public class JoinHandsTogether : ConditionAsset<JoinHandsTogetherInstance>
    {
        [field: SerializeField, Range(0, 180), Tooltip("Minimum angle of vectors of both hands")]
        public float Angle { get; private set; } = 30.0f;
        [field: SerializeField, Range(0, 1), Tooltip("Magnification of the radius for determining if the hand is hit.")]
        public float RadiusRatio { get; private set; } = 0.7f;
        [field: SerializeField, Tooltip("Reverse conditions")]
        public bool IsNot { get; private set; } = false;
    }


    /// <summary>
    /// Determination of the hands which are joined
    /// </summary>
    public class JoinHandsTogetherInstance : ConditionInstance<JoinHandsTogether>
    {


        public bool IsOn { get; private set; } = false;


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            IsOn = true;
            var leftHand = updateInfo.LeftHand;
            var rightHand = updateInfo.RightHand;
            var lVec = (rightHand.Position - leftHand.Position).normalized;
            var rVec = (leftHand.Position - rightHand.Position).normalized;
            IsOn &= Vector3.Angle(leftHand.PalmForward, lVec) < Asset.Angle 
                && Vector3.Angle(rightHand.PalmForward, rVec) < Asset.Angle;
            float len = (leftHand.SpherePosition - rightHand.SpherePosition).magnitude;
            IsOn &= len <= (leftHand.SphereRadius + rightHand.SphereRadius) * Asset.RadiusRatio;
        }


        public override bool GetResult()
        {
            return Asset.IsNot == !IsOn;
        }


    }


}