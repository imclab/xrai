using System;
using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.FingerGun
{


    public class BulletViewModel : BulletView.IViewModel, IDisposable
    {


        public ReadOnlyReactiveProperty<Vector3> ShotPosition => m_shotPosition;
         public ReadOnlyReactiveProperty<bool> IsActive => m_isActive;

        ReactiveProperty<Vector3> m_shotPosition = new();
        ReactiveProperty<bool> m_isActive = new(false);
        Action<Collision> m_onHit;
        CompositeDisposable m_disposable = new();


        public BulletViewModel()
        {
            m_shotPosition.AddTo(m_disposable);
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
        }


        public void Bind(BulletModel model)
        {
            m_shotPosition.Value = model.Position.CurrentValue;
            m_isActive.Value = model.IsActive.CurrentValue;

            model.Position.Subscribe(value => m_shotPosition.Value = value).AddTo(m_disposable);
            model.IsActive.Subscribe(value => m_isActive.Value = value).AddTo(m_disposable);

            m_onHit = model.HitTarget;
        }


        public void Hit(Collision collision)
        {
            m_onHit?.Invoke(collision);
        }


    }


}