using System;
using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Shuriken
{


    public class TargetViewModel : TargetView.IViewModel, IDisposable
    {


        public ReadOnlyReactiveProperty<Vector3> Position => m_position;

        ReactiveProperty<Vector3> m_position = new();
        readonly CompositeDisposable m_disposable = new();


        public TargetViewModel()
        {
            Position.AddTo(m_disposable);
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
        }


        public void Bind(SampleShurikenModel model)
        {
            m_position.Value = model.TargetPosition.CurrentValue;

            model.TargetPosition
                .Subscribe(value => m_position.Value = value)
                .AddTo(m_disposable);
        }


    }


}