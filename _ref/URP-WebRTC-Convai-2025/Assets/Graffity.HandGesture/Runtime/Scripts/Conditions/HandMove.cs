using System;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings for detecting hand movement
    /// </summary>
    [Serializable]
    public class HandMove : ConditionAsset<HandMoveInstance>
    {
        [field: SerializeField, Tooltip("Type of hand")]
        public HandInfo.HandType HandType { get; private set; } = default;

        [field: SerializeField, Tooltip("Maximum movement speed")]
        public float Speed { get; private set; } = default;

        [field: SerializeField, Tooltip("Reverse conditions")]
        public bool IsNot { get; private set; } = default;
    }


    /// <summary>
    /// This class is for judging hand movement
    /// </summary>
    public class HandMoveInstance : ConditionInstance<HandMove>
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
            IsOn = hand.MoveVec.magnitude >= Asset.Speed;
        }


        public override bool GetResult()
        {
            return Asset.IsNot == !IsOn;
        }


    }


}