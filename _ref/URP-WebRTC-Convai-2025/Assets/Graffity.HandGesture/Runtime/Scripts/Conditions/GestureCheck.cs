using System;
using UnityEngine;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings required to check if the specified gesture is being performed
    /// </summary>
    [Serializable]
    public class GestureCheck : ConditionAsset<GestureCheckInstance>
    {
        [field: SerializeField, Tooltip("Gesture assets")]
        public GestureAsset GestureAsset { get; private set; }

        [field: SerializeField, Tooltip("Reverse conditions")]
        public bool IsNot { get; private set; } = false;
    }


    /// <summary>
    /// Verify that the specified gesture is being performed
    /// </summary>
    public class GestureCheckInstance : ConditionInstance<GestureCheck>
    {


        ConditionInstanceRepository ConditionInstanceRepository { get; set; } = new();
        bool IsEnd { get; set; } = false;


        protected override void Setup()
        {
            base.Setup();
            ConditionInstanceRepository.Setup(ConditionInstanceGenerator.Generate(Asset.GestureAsset));
        }


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            ConditionInstanceRepository.Update(updateInfo);
            IsEnd = ConditionInstanceRepository.IsAllMatched();
        }


        public override bool GetResult()
        {
            return Asset.IsNot == !IsEnd;
        }


        public override void Dispose()
        {
            ConditionInstanceRepository.Dispose();
        }


    }


}