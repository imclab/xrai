using System;
using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Beam
{


    public class BeamViewModel : BeamView.IViewModel, IDisposable
    {


        public ReadOnlyReactiveProperty<Vector3> Position => m_position;
        public ReadOnlyReactiveProperty<bool> IsCharge => m_isCharge;
        public ReadOnlyReactiveProperty<float> ChargeScale => m_chargeScale;
        public ReadOnlyReactiveProperty<bool> IsShot => m_isShot;
        public ReadOnlyReactiveProperty<Quaternion> ShotRotation => m_shotRotation;

        ReactiveProperty<Vector3> m_position = new();
        ReactiveProperty<bool> m_isCharge = new();
        ReactiveProperty<float> m_chargeScale = new();
        ReactiveProperty<bool> m_isShot = new();
        ReactiveProperty<Quaternion> m_shotRotation = new();
        CompositeDisposable m_disposable = new();


        public BeamViewModel()
        {
            m_isCharge.AddTo(m_disposable);
            m_chargeScale.AddTo(m_disposable);
            m_isShot.AddTo(m_disposable);
            m_shotRotation.AddTo(m_disposable);
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
        }


        public void Bind(SampleBeamModel model)
        {
            m_position.Value = model.Position.CurrentValue;
            m_isCharge.Value = model.IsCharge.CurrentValue;
            m_chargeScale.Value = model.ChargeValue.CurrentValue;
            m_isShot.Value = model.IsShot.CurrentValue;
            m_shotRotation.Value = model.ShotRotation.CurrentValue;

            model.Position.Subscribe(value => m_position.Value = value).AddTo(m_disposable);
            model.IsCharge.Subscribe(value => m_isCharge.Value = value).AddTo(m_disposable);
            model.ChargeValue.Subscribe(value => m_chargeScale.Value = value + 1f).AddTo(m_disposable);
            model.IsShot.Subscribe(value => m_isShot.Value = value).AddTo(m_disposable);
            model.ShotRotation.Subscribe(value => m_shotRotation.Value = value).AddTo(m_disposable);
        }


    }


}