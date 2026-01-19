using System;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings required to make a decision about the distance between two hands
    /// </summary>
    [Serializable]
    public class HandsDistance : ConditionAsset<HandsDistanceInstance>
    {
        public enum ConditionType
        {
            Below,
            Above,
        }
        [field: SerializeField, Tooltip("Type of condition")]
        public ConditionType Condition { get; private set; } = 0;
        [field: SerializeField, Tooltip("Distance（m）")]
        public float Distance { get; private set; } = 0.1f;
    }


    /// <summary>
    /// Judgment on the distance between two hands
    /// </summary>
    public class HandsDistanceInstance : ConditionInstance<HandsDistance>
    {


        public bool IsOn { get; private set; } = false;


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            float len = (updateInfo.LeftHand.Position - updateInfo.RightHand.Position).magnitude;
            IsOn = Asset.Condition switch
            {
                HandsDistance.ConditionType.Below => len <= Asset.Distance,
                HandsDistance.ConditionType.Above => len >= Asset.Distance,
                _ => false
            };
        }


        public override bool GetResult()
        {
            return IsOn;
        }


    }


}