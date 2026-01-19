using System;
using System.Collections.Generic;
using Graffity.HandGesture.Attributes;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings required for process repetition
    /// </summary>
    [Serializable]
    public class Loop : ConditionAsset<LoopInstance>
    {

        /// <summary> List of conditions to cancel this flow </summary>
        [field: SerializeReference, Space(5), Header("Conditions for terminating the Loop (if any of them are true, the loop will be terminated.)"), ConditionSelector(ConditionSelectorAttribute.ConditionType.Sequence)]
        public List<IConditionAsset> Cancel { get; private set; } = new();

        /// <summary> Check the Index order. </summary>
        [field: SerializeReference, Space(5), Header("Flow (process from top to bottom)"), ConditionSelector(ConditionSelectorAttribute.ConditionType.Sequence)]
        public List<IConditionAsset> Flow { get; private set; } = new();

    }


    /// <summary>
    /// Repeat the process.
    /// </summary>
    public class LoopInstance : ConditionInstance<Loop> , ISequenceEvent , ISequenceEventRepositoryGetter , ISequenceOnlyCondition
    {


        ConditionInstanceRepository CancelInstanceRepository { get; set; } = new();
        ConditionInstanceRepository FlowInstanceRepository { get; set; } = new();
        SequenceEventRepository SequenceEventRepository { get; set; } = new();
        int CurrentIndex { get; set; } = 0;
        int FlowCount { get; set; } = 0;
        bool IsEnd { get; set; } = false;


        protected override void Setup()
        {
            base.Setup();
            CancelInstanceRepository.Setup(ConditionInstanceGenerator.Generate(Asset.Cancel));
            FlowInstanceRepository.Setup(ConditionInstanceGenerator.Generate(Asset.Flow));
            FlowCount = FlowInstanceRepository.ItemList.Count;
            SequenceEventRepository.Setup(CancelInstanceRepository,FlowInstanceRepository);
        }


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            if (IsEnd)
            {
                Initialize();
            }
            // Check interruption.
            CancelInstanceRepository.Update(updateInfo);
            if (CancelInstanceRepository.IsAnyMatched())
            {
                IsEnd = true;
                return;
            }
            // Flow Update
            var flow = FlowInstanceRepository.ItemList[CurrentIndex];
            flow.Update(updateInfo);
            if (!flow.GetResult()) return;
            CurrentIndex++;
            if (FlowCount > CurrentIndex) return;
            CurrentIndex = 0;
        }


        void Initialize()
        {
            IsEnd = false;
            CurrentIndex = 0;
        }


        public void BeginSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
            Initialize();
            SequenceEventRepository.BeginSequence(updateInfo);
        }

        public void CancelSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
            Initialize();
            SequenceEventRepository.CancelSequence(updateInfo);
        }

        public void EndSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
            Initialize();
            SequenceEventRepository.EndSequence(updateInfo);
        }


        public override bool GetResult()
        {
            return IsEnd;
        }


        public override void Dispose()
        {
            CancelInstanceRepository.Dispose();
            FlowInstanceRepository.Dispose();
        }


        public ISequenceEventRepository GetSequenceEventRepository()
        {
            return SequenceEventRepository;
        }


    }


}