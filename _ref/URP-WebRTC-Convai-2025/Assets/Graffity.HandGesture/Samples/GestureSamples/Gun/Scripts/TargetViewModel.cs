using System;
using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.FingerGun
{


    public class TargetViewModel : TargetView.IViewModel, IDisposable
    {


        public ReadOnlyReactiveProperty<Vector3> Position => m_position;

        ReactiveProperty<Vector3> m_position = new();
        Action<Collision> m_onHit;
        readonly CompositeDisposable m_disposable = new();


        public TargetViewModel()
        {
            Position.AddTo(m_disposable);
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
        }


        public void Bind(TargetModel model)
        {
            m_position.Value = model.Position.CurrentValue;

            model.Position
                .Subscribe(value => m_position.Value = value)
                .AddTo(m_disposable);

            m_onHit = model.HitBullet;
        }


        public void Hit(Collision collision)
        {
            m_onHit?.Invoke(collision);
        }


    }


}