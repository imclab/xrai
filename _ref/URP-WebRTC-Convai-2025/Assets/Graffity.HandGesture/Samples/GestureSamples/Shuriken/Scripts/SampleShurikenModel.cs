using System;
using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Shuriken
{


    public class SampleShurikenModel : IDisposable
    {


        public ReadOnlyReactiveProperty<bool> IsShot => m_isShot;
        public ReadOnlyReactiveProperty<Vector3> ShotPosition => m_shotPosition;
        public ReadOnlyReactiveProperty<Quaternion> ShotRotation => m_shotRotation;
        public ReadOnlyReactiveProperty<Vector3> TargetPosition => m_targetPosition;

        ReactiveProperty<bool> m_isShot = new(false);
        ReactiveProperty<Vector3> m_shotPosition = new();
        ReactiveProperty<Quaternion> m_shotRotation = new();
        ReactiveProperty<Vector3> m_targetPosition = new();
        Vector3 m_shotVec;
        readonly CompositeDisposable m_disposable = new();

        const float SHOT_SPEED = 0.06f;
        static readonly Vector2 TARGET_SPAWN_X_RANGE = new(-1f, 1f);
        static readonly Vector2 TARGET_SPAWN_Y_RANGE = new(1f, 2f);
        static readonly Vector2 TARGET_SPAWN_Z_RANGE = new(2f, 3f);


        public SampleShurikenModel()
        {
            m_isShot.AddTo(m_disposable);
            m_shotPosition.AddTo(m_disposable);
            m_targetPosition.AddTo(m_disposable);

            InitializeTarget();
        }


        public void Update()
        {
            if (!m_isShot.Value) return;
            m_shotPosition.Value += m_shotVec;
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
        }


        public void HitShuriken(Collision collision)
        {
            if (collision.collider.tag == "Target")
            {
                InitializeTarget();
            }
        }


        public void Shot(GestureManager gestureManager)
        {
            m_isShot.Value = true;
            var hand = Vector3.Angle(gestureManager.LeftHandInfo.PalmForward, Vector3.up) < 90f ?
                gestureManager.LeftHandInfo : gestureManager.RightHandInfo;
            m_shotPosition.Value = hand.Position;
            if (hand.TryGetJointInfo(UnityEngine.XR.Hands.XRHandJointID.MiddleTip, out var joint_mt))
            {
                m_shotVec = joint_mt.CurrentPose.forward * SHOT_SPEED;
            }
            m_shotRotation.Value = Quaternion.LookRotation(m_shotVec.normalized,hand.PalmForward);
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