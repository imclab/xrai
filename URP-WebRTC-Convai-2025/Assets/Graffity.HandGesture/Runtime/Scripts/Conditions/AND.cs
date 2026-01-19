using System;
using System.Collections.Generic;
using Graffity.HandGesture.Attributes;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings required to determine if all conditions match.
    /// </summary>
    [Serializable]
    public class AND : ConditionAsset<ANDInstance>
    {
        [field: SerializeReference, Tooltip("List of gesture conditions"), ConditionSelector]
        public List<IConditionAsset> ConditionAssetList { get; private set; } = new();
        
        [field: SerializeField, Tooltip("Reverse conditions")]
        public bool IsNot { get; private set; } = false;
    }


    /// <summary>
    /// Determine if all conditions match.
    /// </summary>
    public class ANDInstance : ConditionInstance<AND> , ISequenceEventRepositoryGetter
    {


        public ConditionInstanceRepository ConditionInstanceRepository { get; private set; } = new();
        SequenceEventRepository SequenceEventRepository { get; set; } = new();
        public bool IsOn { get; private set; } = false;


        protected override void Setup()
        {
            base.Setup();
            ConditionInstanceRepository.Setup(ConditionInstanceGenerator.Generate(Asset.ConditionAssetList));
            SequenceEventRepository.Setup(ConditionInstanceRepository);
        }


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            ConditionInstanceRepository.Update(updateInfo);
            IsOn = ConditionInstanceRepository.IsAllMatched();
        }


        public override bool GetResult()
        {
            return Asset.IsNot == !IsOn;
        }

        public override void Dispose()
        {
            ConditionInstanceRepository.Dispose();
        }

        public ISequenceEventRepository GetSequenceEventRepository()
        {
            return SequenceEventRepository;
        }


    }


}