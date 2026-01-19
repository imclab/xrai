using System;
using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Shuriken
{


    public class ShurikenViewModel : ShurikenView.IViewModel, IDisposable
    {


        public ReadOnlyReactiveProperty<bool> IsShot => m_isShot;
        public ReadOnlyReactiveProperty<Vector3> ShotPosition => m_shotPosition;
        public ReadOnlyReactiveProperty<Quaternion> ShotRotation => m_shotRotation;

        ReactiveProperty<bool> m_isShot = new();
        ReactiveProperty<Vector3> m_shotPosition = new();
        ReactiveProperty<Quaternion> m_shotRotation = new();
        Action<Collision> m_onHit;
        CompositeDisposable m_disposable = new();


        public ShurikenViewModel()
        {
            m_isShot.AddTo(m_disposable);
            m_shotPosition.AddTo(m_disposable);
            m_shotRotation.AddTo(m_disposable);
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
        }


        public void Bind(SampleShurikenModel model)
        {
            m_isShot.Value = model.IsShot.CurrentValue;
            m_shotPosition.Value = model.ShotPosition.CurrentValue;
            m_shotRotation.Value = model.ShotRotation.CurrentValue;

            model.IsShot.Subscribe(value => m_isShot.Value = value).AddTo(m_disposable);
            model.ShotPosition.Subscribe(value => m_shotPosition.Value = value).AddTo(m_disposable);
            model.ShotRotation.Subscribe(value => m_shotRotation.Value = value).AddTo(m_disposable);

            m_onHit = model.HitShuriken;
        }


        public void Hit(Collision collision)
        {
            m_onHit?.Invoke(collision);
        }


    }


}