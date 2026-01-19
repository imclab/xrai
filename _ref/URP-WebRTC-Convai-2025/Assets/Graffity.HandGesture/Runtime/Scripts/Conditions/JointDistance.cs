using System;
using UnityEngine;
using Graffity.HandGesture.Conditions.Data;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings required to determine the distance between two joints.
    /// </summary>
    [Serializable]
    public class JointDistance : ConditionAsset<JointDistanceInstance>
    {
        [field: SerializeField, Tooltip("Joint 1 setting")]
        public JointData Joint1 { get; private set; } = default;
        [field: SerializeField, Tooltip("Joint 2 setting")]
        public JointData Joint2 { get; private set; } = default;
        [field: SerializeField, Min(0.0f), Tooltip("Minimum distance between two joints（m）")]
        public float Min { get; private set; } = 0;
        [field: SerializeField, Min(0.0f), Tooltip("Maximum distance between two joints（m）")]
        public float Max { get; private set; } = 1;
    }


    /// <summary>
    /// Determine the distance between two joints.
    /// </summary>
    public class JointDistanceInstance : ConditionInstance<JointDistance>
    {


        public bool IsOn { get; private set; } = false;
        float Min { get; set; }
        float Max { get; set; }


        protected override void Setup()
        {
            base.Setup();
            Min = Asset.Min * Asset.Min;
            Max = Asset.Max * Asset.Max;
        }


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            if (!Asset.Joint1.TryGetJointInfo(updateInfo.LeftHand, updateInfo.RightHand, out var jointInfo1)
                || !Asset.Joint2.TryGetJointInfo(updateInfo.LeftHand, updateInfo.RightHand, out var jointInfo2))
            {
                IsOn = false;
                return;
            }
            float len = Vector3.SqrMagnitude(jointInfo1.CurrentPose.position - jointInfo2.CurrentPose.position);
            IsOn = Min <= len && len <= Max;
        }


        public override bool GetResult()
        {
            return IsOn;
        }


    }


}