using System;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings to update GestureState
    /// </summary>
    [Serializable]
    public class GestureStateUpdate : ConditionAsset<GestureStateUpdateInstance>
    {
        [field: SerializeField, Tooltip("New StateName")]
        public string NewState { get; private set; }
    }


    /// <summary>
    /// Update GestureState.
    /// </summary>
    public class GestureStateUpdateInstance : ConditionInstance<GestureStateUpdate>
    {


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            updateInfo.GestureState.UpdateState(Asset.NewState);
        }


        public override bool GetResult()
        {
            return true;
        }


    }


}