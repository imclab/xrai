using System;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings for determine the hand-move-distance
    /// </summary>
    [Serializable]
    public class HandMoveDistance : ConditionAsset<HandMoveDistanceInstance>
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
        [field: SerializeField, Tooltip("distance（m）")]
        public float Distance { get; private set; } = 0.1f;
        [field: SerializeField, Tooltip("Criteria for Rotation")]
        public RotationType RotationBase { get; private set; } = RotationType.World;
        [field: SerializeField, Tooltip("Reverse conditions")]
        public bool IsNot { get; private set; } = default;
    }


    /// <summary>
    /// Determine hand move distance.
    /// </summary>
    public class HandMoveDistanceInstance : ConditionInstance<HandMoveDistance>, ISequenceEvent
    {


        public bool IsOn { get; private set; } = false;
        Vector3 BasePosition { get; set; } = default;
        Transform CameraTransform { get; set; } = null;


        protected override void Setup()
        {
            base.Setup();
            CameraTransform = Camera.main.transform;
        }


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            var hand = Asset.HandType switch
            {
                HandInfo.HandType.Left => updateInfo.LeftHand,
                HandInfo.HandType.Right => updateInfo.RightHand,
                _ => null
            };
            if (hand == null) return;
            var moveVec = hand.Position - BasePosition;
            moveVec = Asset.RotationBase switch
            {
                HandMoveDistance.RotationType.World => moveVec,
                HandMoveDistance.RotationType.Local => hand.Rotation * moveVec.normalized * moveVec.magnitude,
                HandMoveDistance.RotationType.CameraLocal => CameraTransform.InverseTransformDirection(moveVec.normalized) * moveVec.magnitude,
                _ => Vector3.zero
            };
            IsOn = Asset.Direction switch
            {
                HandMoveDistance.DirectionType.Up => moveVec.y > Asset.Distance,
                HandMoveDistance.DirectionType.Down => moveVec.y < -Asset.Distance,
                HandMoveDistance.DirectionType.Left => moveVec.x < -Asset.Distance,
                HandMoveDistance.DirectionType.Right => moveVec.x > Asset.Distance,
                HandMoveDistance.DirectionType.Front => moveVec.z > Asset.Distance,
                HandMoveDistance.DirectionType.Back => moveVec.z < -Asset.Distance,
                _ => false,
            };
        }


        public override bool GetResult()
        {
            return Asset.IsNot == !IsOn;
        }


        public void BeginSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
            var hand = Asset.HandType switch
            {
                HandInfo.HandType.Left => updateInfo.LeftHand,
                HandInfo.HandType.Right => updateInfo.RightHand,
                _ => null
            };
            if (hand == null) return;
            BasePosition = hand.Position;
        }

        public void CancelSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
        }

        public void EndSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
        }


    }


}