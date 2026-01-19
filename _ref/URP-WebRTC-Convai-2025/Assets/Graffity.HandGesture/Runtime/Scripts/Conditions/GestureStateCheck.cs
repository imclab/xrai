using System;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Set to check if the specified GestureState is a match.
    /// </summary>
    [Serializable]
    public class GestureStateCheck : ConditionAsset<GestureStateCheckInstance>
    {
        [field: SerializeField, Tooltip("StateName to be checked")]
        public string State { get; private set; }

        [field: SerializeField, Tooltip("Reverse conditions")]
        public bool IsNot { get; private set; } = false;
    }


    /// <summary>
    /// Determine if GestureState matches.
    /// </summary>
    public class GestureStateCheckInstance : ConditionInstance<GestureStateCheck>
    {


        bool IsOn { get; set; } = false;
        int HashCode { get; set; }


        protected override void Setup()
        {
            base.Setup();
            HashCode = Asset.State.GetHashCode();
        }


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            IsOn = updateInfo.GestureState.StateHashCode == HashCode;
        }


        public override bool GetResult()
        {
            return Asset.IsNot == !IsOn;
        }


    }


}