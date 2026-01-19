using System;
using UnityEngine;
using Graffity.HandGesture.Conditions.Data;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Settings required to determine joint rotation
    /// </summary>
    [Serializable]
    public class JointRotation : ConditionAsset<JointRotationInstance>
    {
        static readonly float PITCH_MIN = -100.0f;
        static readonly float PITCH_MAX = 55.0f;
        static readonly float YAW_MIN = -180.0f;
        static readonly float YAW_MAX = 180.0f;
        [field: SerializeField, Tooltip("Joint Settings")]
        public JointData Joint { get; private set; } = default;
        [field: SerializeField, Range(-180, 180), Tooltip("Minimum rotation value")]
        public float PitchMin { get; private set; } = PITCH_MIN;
        [field: SerializeField, Range(-180, 180), Tooltip("Maximum rotation value")]
        public float PitchMax { get; private set; } = PITCH_MAX;
        [field: SerializeField, Range(-180, 180), Tooltip("Minimum rotation value")]
        public float YawMin { get; private set; } = YAW_MIN;
        [field: SerializeField, Range(-180, 180), Tooltip("Maximum rotation value")]
        public float YawMax { get; private set; } = YAW_MAX;
        [field: SerializeField, Tooltip("Whether world coordinates are used as a reference or not")]
        public bool IsWorldRotation { get; private set; } = false;
    }


    /// <summary>
    /// Determination of joint rotation
    /// </summary>
    public class JointRotationInstance : ConditionInstance<JointRotation>
    {


        public bool IsOn { get; private set; } = false;


        public override void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            if (!Asset.Joint.TryGetJointInfo(updateInfo.LeftHand, updateInfo.RightHand, out var jointInfo)) return;
            var eulerAngles = Asset.IsWorldRotation ? jointInfo.WorldRotationNormalizedEulerAngles : jointInfo.LocalRotationNormalizedEulerAngles;
            IsOn = Asset.PitchMin < eulerAngles.x && eulerAngles.x < Asset.PitchMax
                && Asset.YawMin < eulerAngles.y && eulerAngles.y < Asset.YawMax;
        }


        public override bool GetResult()
        {
            return IsOn;
        }


    }


}