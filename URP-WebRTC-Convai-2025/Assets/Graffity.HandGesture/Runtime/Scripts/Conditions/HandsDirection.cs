using System;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings required to determine the orientation of the two hands
    /// </summary>
    [Serializable]
    public class HandsDirection : ConditionAsset<HandsDirectionInstance>
    {
        public enum DirectionType
        {
            /// <summary> same orientation </summary>
            Same,
            /// <summary> facing the opposite direction </summary>
            Reverse,
            /// <summary> Facing each other. </summary>
            FaceEachOther,
        }
        [field: SerializeField, Tooltip("Direction Type")]
        public DirectionType Direction { get; private set; } = 0;
        [field: SerializeField, Range(0.0f, 180.0f), Tooltip("Maximum Angle")]
        public float Angle { get; private set; } = 90;
        [field: SerializeField, Tooltip("Reverse conditions")]
        public bool IsNot { get; private set; } = false;
    }


    /// <summary>
    /// Determine the orientation of the two hands
    /// </summary>
    public class HandsDirectionInstance : ConditionInstance<HandsDirection>
    {


        public bool IsOn { get; private set; } = false;


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            IsOn = Asset.Direction switch
            {
                HandsDirection.DirectionType.Same => Same(updateInfo.LeftHand, updateInfo.RightHand),
                HandsDirection.DirectionType.Reverse => Reverse(updateInfo.LeftHand, updateInfo.RightHand),
                HandsDirection.DirectionType.FaceEachOther => FaceEachOther(updateInfo.LeftHand, updateInfo.RightHand),
                _ => false
            };
        }

        bool Same(HandInfo leftHand, HandInfo rightHand)
        {
            return Vector3.Angle(leftHand.PalmForward, rightHand.PalmForward) < Asset.Angle;
        }

        bool Reverse(HandInfo leftHand, HandInfo rightHand)
        {
            return Vector3.Angle(leftHand.PalmForward, -rightHand.PalmForward) < Asset.Angle;
        }

        bool FaceEachOther(HandInfo leftHand, HandInfo rightHand)
        {
            var lVec = (rightHand.Position - leftHand.Position).normalized;
            var rVec = (leftHand.Position - rightHand.Position).normalized;
            return Vector3.Angle(leftHand.PalmForward, lVec) < Asset.Angle &&
                Vector3.Angle(rightHand.PalmForward, rVec) < Asset.Angle;
        }


        public override bool GetResult()
        {
            return Asset.IsNot == !IsOn;
        }


    }


}