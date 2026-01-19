using System;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings required to determine hand position
    /// </summary>
    [Serializable]
    public class HandPosition : ConditionAsset<HandPositionInstance>
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
        [field: SerializeField, Tooltip("Type of hand")]
        public HandInfo.HandType HandType { get; private set; } = default;
        [field: SerializeField, Tooltip("Threshold position (relative to camera)")]
        public Vector3 ThresholdPosition {get; private set; } = Vector3.zero;
        [field: SerializeField, Tooltip("Type of direction to determine if the threshold position is exceeded.")]
        public DirectionType Direction { get; private set; } = 0;
    }


    /// <summary>
    /// Determine hand coordinates.
    /// </summary>
    public class HandPositionInstance : ConditionInstance<HandPosition>
    {


        public bool IsOn { get; private set; } = false;
        Transform CameraTransform { get; set; } = null;


        protected override void Setup()
        {
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
            var thresholdPosition = CameraTransform.localPosition + Asset.ThresholdPosition;
            IsOn = Asset.Direction switch
            {
                HandPosition.DirectionType.Up => hand.Position.y > thresholdPosition.y,
                HandPosition.DirectionType.Down => hand.Position.y < thresholdPosition.y,
                HandPosition.DirectionType.Left => hand.Position.x < thresholdPosition.x,
                HandPosition.DirectionType.Right => hand.Position.x > thresholdPosition.x,
                HandPosition.DirectionType.Front => hand.Position.z > thresholdPosition.z,
                HandPosition.DirectionType.Back => hand.Position.z < thresholdPosition.z,
                _ => false,
            };
        }


        public override bool GetResult()
        {
            return IsOn;
        }


    }


}