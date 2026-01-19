using System;
using UnityEngine;
using Graffity.HandGesture.Conditions;
using R3;


namespace Graffity.HandGesture
{


    /// <summary>
    /// Class that manages a single gesture
    /// </summary>
    [Serializable]
    public class Gesture : IGestureUpdater, IGestureResult, IGestureState, IDisposable
    {


        /// <summary> gesture unique ID </summary>
        [field: SerializeField]
        public string ID { get; private set; }

        /// <summary> Gesture assets (ScriptableObject) </summary>
        [field: SerializeField]
        GestureAsset AssetData { get; set; }

        /// <summary> Current information on both hands used for updates </summary>
        public IGestureUpdater.UpdateInfo CurrentUpdateInfo { get; private set; }

        ConditionInstanceRepository ConditionInstanceRepository { get; set; } = new();

        /// <summary> 
        /// Initialize Gesture.
        /// Be sure to perform this before use.
        /// </summary>
        public void Setup()
        {
            ConditionInstanceRepository.Setup(ConditionInstanceGenerator.Generate(AssetData));
        }


        // IGestureUpdater
        /// <summary>
        /// Hand tracking update event.
        /// This method must be called as a callback event of XRHandSubsystem.updatedHands
        /// </summary>
        /// <param name="updateInfo"> Information required for update </param>
        public void OnUpdatedHands(IGestureUpdater.UpdateInfo updateInfo)
        {
            CurrentUpdateInfo = updateInfo;
            // Converted to data for IConditionUpdate
            var conditionUpdateInfo = new IConditionUpdater.UpdateInfo(updateInfo.Subsystem,
                updateInfo.UpdateSuccessFlags,
                updateInfo.UpdateType,
                updateInfo.LeftHand,
                updateInfo.RightHand,
                this);
            // Update Conditions
            ConditionInstanceRepository.Update(conditionUpdateInfo);
            // Check all conditions
            Result = ConditionInstanceRepository.IsAllMatched();
            var inputEvent = Result ? m_onTrueResult : m_onFalseResult;
            inputEvent.OnNext(CurrentUpdateInfo);
        }


        // IGestureResult
        bool m_result = false;
        Subject<(bool, IGestureUpdater.UpdateInfo)> m_onChangeResult = new();
        Subject<IGestureUpdater.UpdateInfo> m_onTrueResult = new();
        Subject<IGestureUpdater.UpdateInfo> m_onFalseResult = new();

        /// <summary> Are all conditions met? </summary>
        public bool Result
        {
            get => m_result;
            private set
            {
                if (m_result == value) return;
                m_result = value;
                m_onChangeResult.OnNext((value, CurrentUpdateInfo));
            }
        }
        /// <summary> Event when Result is changed </summary>
        public Observable<(bool result, IGestureUpdater.UpdateInfo updateInfo)> OnChangeResult() => m_onChangeResult.AsObservable();
        /// <summary> Event when Result is true </summary>
        public Observable<IGestureUpdater.UpdateInfo> OnTrueResult() => m_onTrueResult.AsObservable();
        /// <summary> Event when Result is false </summary>
        public Observable<IGestureUpdater.UpdateInfo> OnFalseResult() => m_onFalseResult.AsObservable();


        // IGestureState
        string m_stateName = default;
        int m_stateHashCode = 0;
        Subject<(string, IGestureUpdater.UpdateInfo)> m_onChangeState = new();

        /// <summary> State Name </summary>
        public string StateName => m_stateName;
        /// <summary> State hash code </summary>
        public int StateHashCode => m_stateHashCode;
        /// <summary>
        /// Update state
        /// </summary>
        /// <param name="newState"> New State Name </param>
        public void UpdateState(string newState)
        {
            int hash = newState.GetHashCode();
            if (m_stateHashCode == hash) return;
            m_stateHashCode = hash;
            m_stateName = newState;
            m_onChangeState.OnNext((newState, CurrentUpdateInfo));
        }
        /// <summary> Event when state changes </summary>
        public Observable<(string newState, IGestureUpdater.UpdateInfo updateInfo)> OnChangeState() => m_onChangeState.AsObservable();


        // IDisposable
        public void Dispose()
        {
            m_onChangeResult.Dispose();
            m_onTrueResult.Dispose();
            m_onFalseResult.Dispose();
            m_onChangeState.Dispose();
            ConditionInstanceRepository.Dispose();
        }


    }


}