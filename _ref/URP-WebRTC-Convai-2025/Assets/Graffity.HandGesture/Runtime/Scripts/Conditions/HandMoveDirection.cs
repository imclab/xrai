using System;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings required to determine the direction of hand movement
    /// </summary>
    [Serializable]
    public class HandMoveDirection : ConditionAsset<HandMoveDirectionInstance>
    {
        public enum DirectionType
        {
            Up,
            Down,
            Left,
            Right,
            Front,
            Back,
        }
        public enum RotationType
        {
            World,
            Local,
            CameraLocal
        }
        [field: SerializeField, Tooltip("Type of hand")]
        public HandInfo.HandType HandType { get; private set; } = default;
        [field: SerializeField, Tooltip("Type of movement direction")]
        public DirectionType Direction { get; private set; } = 0;
        [field: SerializeField, Range(0.0f, 180.0f), Tooltip("Maximum Angle")]
        public float Angle { get; private set; } = 90;
        [field: SerializeField, Tooltip("Criteria for Rotation")]
        public RotationType RotationBase { get; private set; } = RotationType.World;
        [field: SerializeField, Tooltip("Reverse conditions")]
        public bool IsNot { get; private set; } = default;
    }


    /// <summary>
    /// Class to determine the direction of hand movement
    /// </summary>
    public class HandMoveDirectionInstance : ConditionInstance<HandMoveDirection>
    {


        public bool IsOn { get; private set; } = false;


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            var hand = Asset.HandType switch
            {
                HandInfo.HandType.Left => updateInfo.LeftHand,
                HandInfo.HandType.Right => updateInfo.RightHand,
                _ => null
            };
            if (hand == null) return;
            var moveVec = Asset.RotationBase switch
            {
                HandMoveDirection.RotationType.World => hand.MoveVec.normalized,
                HandMoveDirection.RotationType.Local => hand.LocalMoveVec.normalized,
                HandMoveDirection.RotationType.CameraLocal => hand.CameraLocalMoveVec.normalized,
                _ => Vector3.zero
            };
            IsOn = Asset.Direction switch
            {
                HandMoveDirection.DirectionType.Up => Vector3.Angle(moveVec, Vector3.up) < Asset.Angle,
                HandMoveDirection.DirectionType.Down => Vector3.Angle(moveVec, Vector3.down) < Asset.Angle,
                HandMoveDirection.DirectionType.Left => Vector3.Angle(moveVec, Vector3.left) < Asset.Angle,
                HandMoveDirection.DirectionType.Right => Vector3.Angle(moveVec, Vector3.right) < Asset.Angle,
                HandMoveDirection.DirectionType.Front => Vector3.Angle(moveVec, Vector3.forward) < Asset.Angle,
                HandMoveDirection.DirectionType.Back => Vector3.Angle(moveVec, Vector3.back) < Asset.Angle,
                _ => false,
            };
        }


        public override bool GetResult()
        {
            return Asset.IsNot == !IsOn;
        }


    }


}