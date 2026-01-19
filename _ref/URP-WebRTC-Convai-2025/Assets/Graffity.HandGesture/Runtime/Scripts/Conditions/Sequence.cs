using System;
using System.Collections.Generic;
using System.Linq;
using Graffity.HandGesture.Attributes;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Required settings for gesture flow
    /// </summary>
    [Serializable]
    public class Sequence : ConditionAsset<SequenceInstance>
    {

        [field: SerializeField, Space(5), Header("Starting conditions"), Tooltip("If a state name is set, the GestureState will be updated at the start of the sequence. If it is an empty string, the process will not be executed.")]
        public string BeginState { get; private set; } = "Begin";
        [field: SerializeReference, Tooltip("Conditions for starting the flow (if all are met, start)"), ConditionSelector(ConditionSelectorAttribute.ConditionType.Sequence)]
        public List<IConditionAsset> Trigger { get; private set; } = new();

        [field: SerializeField, Space(5), Header("Cancellation conditions"), Tooltip("If a state name is set, the GestureState will be updated at the cancellation timing of the sequence. If it is an empty string, the process will not be executed.")]
        public string CancelState { get; private set; } = "Cancel";
        [field: SerializeReference, Tooltip("List of conditions to cancel this flow (if any of the conditions are met, current sequence will be interrupted)"), ConditionSelector(ConditionSelectorAttribute.ConditionType.Sequence)]
        public List<IConditionAsset> Cancel { get; private set; } = new();

        [field: SerializeReference, Space(5), Header("Flow (process from top to bottom)"), Tooltip("Check the Index order."),ConditionSelector(ConditionSelectorAttribute.ConditionType.Sequence)]
        public List<IConditionAsset> Flow { get; private set; } = new();

        [field: SerializeField, Space(5), Header("End"), Tooltip("If a state name is set, the GestureState will be updated at the end timing of the sequence. If it is an empty string, the process will not be executed.")]
        public string FinishState { get; private set; } = "Finish";
    }


    /// <summary>
    /// Classes to do gesture flow
    /// </summary>
    public class SequenceInstance : ConditionInstance<Sequence>, ISequenceEvent
    {


        enum StateType
        {
            Stop,
            Play,
        }


        ConditionInstanceRepository TriggerInstanceRepository { get; set; } = new();
        ConditionInstanceRepository CancelInstanceRepository { get; set; } = new();
        ConditionInstanceRepository FlowInstanceRepository { get; set; } = new();
        SequenceEventRepository SequenceEventRepository { get; set; } = new();
        StateType State { get; set; } = StateType.Stop;
        int CurrentIndex { get; set; } = 0;
        int FlowCount { get; set; } = 0;
        bool IsEnd { get; set; } = false;
        IConditionUpdater.UpdateInfo UpdateInfo { get; set; }


        protected override void Setup()
        {
            base.Setup();
            TriggerInstanceRepository.Setup(ConditionInstanceGenerator.Generate(Asset.Trigger));
            CancelInstanceRepository.Setup(ConditionInstanceGenerator.Generate(Asset.Cancel));
            FlowInstanceRepository.Setup(ConditionInstanceGenerator.Generate(Asset.Flow));
            FlowCount = FlowInstanceRepository.ItemList.Count;
            SequenceEventRepository.Setup(TriggerInstanceRepository, CancelInstanceRepository, FlowInstanceRepository);
        }


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            UpdateInfo = updateInfo;
            TriggerUpdate();
            FlowUpdate();
        }


        void TriggerUpdate()
        {
            if (State != StateType.Stop) return;
            // If no Trigger, playback as is
            if (TriggerInstanceRepository.ItemList.Any())
            {
                TriggerInstanceRepository.Update(UpdateInfo);
                // all triggers are required to be matched
                if (!TriggerInstanceRepository.IsAllMatched())
                {
                    return;
                }
            }
            // Sequence playback
            Play();
        }

        void FlowUpdate()
        {
            if (FlowCount > CurrentIndex)
            {
                if (State != StateType.Play) return;
                // Check for interruption
                CancelInstanceRepository.Update(UpdateInfo);
                if (CancelInstanceRepository.IsAnyMatched())
                {
                    Cancel();
                    return;
                }
                // Flow Update
                var flow = FlowInstanceRepository.ItemList[CurrentIndex];
                flow.Update(UpdateInfo);
                if (!flow.GetResult()) return;
                CurrentIndex++;
                if (FlowCount > CurrentIndex) return;
            }
            End();
        }


        void Initialize()
        {
            CurrentIndex = 0;
            State = StateType.Stop;
        }


        void Play()
        {
            CurrentIndex = 0;
            State = StateType.Play;
            IsEnd = false;
            SequenceEventRepository.BeginSequence(UpdateInfo);
            if (string.IsNullOrEmpty(Asset.BeginState)) return;
            UpdateInfo.GestureState.UpdateState(Asset.BeginState);
        }

        void Cancel()
        {
            Initialize();
            SequenceEventRepository.CancelSequence(UpdateInfo);
            if (string.IsNullOrEmpty(Asset.CancelState)) return;
            UpdateInfo.GestureState.UpdateState(Asset.CancelState);
        }

        void End()
        {
            Initialize();
            IsEnd = true;
            SequenceEventRepository.EndSequence(UpdateInfo);
            if (string.IsNullOrEmpty(Asset.FinishState)) return;
            UpdateInfo.GestureState.UpdateState(Asset.FinishState);
        }


        public void BeginSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
            Initialize();
        }

        public void CancelSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
            Initialize();
            // When the sequence canceled, all children conditions must be notified the cancel event
            SequenceEventRepository.CancelSequence(UpdateInfo);
        }

        public void EndSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
            Initialize();
        }


        public override bool GetResult()
        {
            return IsEnd;
        }


        public override void Dispose()
        {
            TriggerInstanceRepository.Dispose();
            CancelInstanceRepository.Dispose();
            FlowInstanceRepository.Dispose();
        }


    }


}