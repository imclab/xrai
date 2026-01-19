using System;
using System.Collections.Generic;
using Graffity.HandGesture.Attributes;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings required to determine if any of the conditions match.
    /// </summary>
    [Serializable]
    public class OR : ConditionAsset<ORInstance>
    {
        [field: SerializeReference, Tooltip("List of gesture conditions"), ConditionSelector]
        public List<IConditionAsset> ConditionAssetList { get; private set; } = new();

        [field: SerializeField, Tooltip("Reverse conditions")]
        public bool IsNot { get; private set; } = default;
    }


    /// <summary>
    /// Determine if any of the conditions match.
    /// </summary>
    public class ORInstance : ConditionInstance<OR>, ISequenceEventRepositoryGetter
    {


        ConditionInstanceRepository ConditionInstanceRepository { get; set; } = new();
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
            IsOn = ConditionInstanceRepository.IsAnyMatched();
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