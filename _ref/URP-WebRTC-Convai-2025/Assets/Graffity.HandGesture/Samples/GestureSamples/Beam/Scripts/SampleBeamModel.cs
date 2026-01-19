using System;
using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Beam
{


    public class SampleBeamModel : IDisposable
    {


        public ReadOnlyReactiveProperty<Vector3> Position => m_position;
        public ReadOnlyReactiveProperty<bool> IsCharge => m_isCharge;
        public ReadOnlyReactiveProperty<float> ChargeValue => m_chargeValue;
        public ReadOnlyReactiveProperty<bool> IsShot => m_isShot;
        public ReadOnlyReactiveProperty<Quaternion> ShotRotation => m_shotRotation;
        public ReadOnlyReactiveProperty<Vector3> TargetPosition => m_targetPosition;

        ReactiveProperty<Vector3> m_position = new();
        ReactiveProperty<bool> m_isCharge = new(false);
        ReactiveProperty<float> m_chargeValue = new(1f);
        ReactiveProperty<bool> m_isShot = new(false);
        ReactiveProperty<Quaternion> m_shotRotation = new();
        ReactiveProperty<Vector3> m_targetPosition = new();
        readonly CompositeDisposable m_disposable = new();

        const float MAX_CHARGE_VALUE = 1f;
        const float CHARGE_SPEED = 0.01f;
        const float SHOT_POSITION_OFFSET = 0.2f;
        static readonly Vector2 TARGET_SPAWN_X_RANGE = new(-1f, 1f);
        static readonly Vector2 TARGET_SPAWN_Y_RANGE = new(1f, 2f);
        static readonly Vector2 TARGET_SPAWN_Z_RANGE = new(5f, 7f);


        public SampleBeamModel()
        {
            m_position.AddTo(m_disposable);
            m_isCharge.AddTo(m_disposable);
            m_chargeValue.AddTo(m_disposable);
            m_isShot.AddTo(m_disposable);
            m_shotRotation.AddTo(m_disposable);
            m_targetPosition.AddTo(m_disposable);

            InitializeTarget();
        }


        public void Update(GestureManager gestureManager)
        {
            UpdateCharge(gestureManager);
            UpdateShot(gestureManager);
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
        }


        public void HitTarget(Collider collider)
        {
            if (collider.tag == "Beam")
            {
                InitializeTarget();
            }
        }


        public void SetChargeFlag(bool flag)
        {
            if (!m_isCharge.Value && flag)
            {
                m_chargeValue.Value = 0f;
            }
            m_isCharge.Value = flag;
        }
        void UpdateCharge(GestureManager gestureManager)
        {
            if (!m_isCharge.Value) return;
            if (m_chargeValue.Value < MAX_CHARGE_VALUE)
            {
                m_chargeValue.Value += CHARGE_SPEED;
            }
            bool isValid = true;
            isValid &= gestureManager.LeftHandInfo.TryGetJointInfo(UnityEngine.XR.Hands.XRHandJointID.MiddleProximal, out var left_mp);
            isValid &= gestureManager.RightHandInfo.TryGetJointInfo(UnityEngine.XR.Hands.XRHandJointID.MiddleProximal, out var right_mp);
            if (!isValid) return;
            m_position.Value = Vector3.Lerp(left_mp.CurrentPose.position, right_mp.CurrentPose.position, 0.5f);
        }


        public void SetShotFlag(bool flag)
        {
            m_isShot.Value = flag;
        }
        void UpdateShot(GestureManager gestureManager)
        {
            if (!m_isShot.Value) return;
            bool isValid = true;
            isValid &= gestureManager.LeftHandInfo.TryGetJointInfo(UnityEngine.XR.Hands.XRHandJointID.Wrist, out var left_wrist);
            isValid &= gestureManager.RightHandInfo.TryGetJointInfo(UnityEngine.XR.Hands.XRHandJointID.Wrist, out var right_wrist);
            if (!isValid) return;
            var vec1 = right_wrist.CurrentPose.position - left_wrist.CurrentPose.position;
            var vec2 = left_wrist.CurrentPose.right;
            var vec3 = Vector3.Cross(vec1.normalized, vec2.normalized);
            m_position.Value = Vector3.Lerp(left_wrist.CurrentPose.position, right_wrist.CurrentPose.position, 0.5f) + vec3 * SHOT_POSITION_OFFSET;
            var lookAtRotation = Quaternion.LookRotation(vec3, Vector3.up);
            var offsetRotation = Quaternion.FromToRotation(Vector3.forward, Vector3.forward);
            m_shotRotation.Value = lookAtRotation * offsetRotation;
        }


        void InitializeTarget()
        {
            m_targetPosition.Value = new Vector3(
                UnityEngine.Random.Range(TARGET_SPAWN_X_RANGE.x, TARGET_SPAWN_X_RANGE.y),
                UnityEngine.Random.Range(TARGET_SPAWN_Y_RANGE.x, TARGET_SPAWN_Y_RANGE.y),
                UnityEngine.Random.Range(TARGET_SPAWN_Z_RANGE.x, TARGET_SPAWN_Z_RANGE.y));
        }


    }


}