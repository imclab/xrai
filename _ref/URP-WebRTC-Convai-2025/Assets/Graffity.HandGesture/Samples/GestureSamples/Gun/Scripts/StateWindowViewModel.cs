using System;
using R3;


namespace Graffity.HandGesture.Sample.FingerGun
{


    public class StateWindowViewModel : StateWindowView.IViewModel, IDisposable
    {


        public ReadOnlyReactiveProperty<string> GestureName => m_gestureName;
        public ReadOnlyReactiveProperty<string> CurrentState => m_currentState;
        public ReadOnlyReactiveProperty<bool> IsLeft => m_isLeft;

        ReactiveProperty<string> m_gestureName = new();
        ReactiveProperty<string> m_currentState = new();
        ReactiveProperty<bool> m_isLeft = new();
        readonly CompositeDisposable m_disposable = new();


        public StateWindowViewModel(bool isLeft)
        {
            m_gestureName.AddTo(m_disposable);
            m_currentState.AddTo(m_disposable);
            m_isLeft.AddTo(m_disposable);
            m_isLeft.Value = isLeft;
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
        }


        public void Bind(Gesture gesture)
        {
            m_gestureName.Value = gesture.ID;
            m_currentState.Value = gesture.StateName;

            gesture.OnChangeState()
                .Subscribe(value => m_currentState.Value = value.newState)
                .AddTo(this.m_disposable);
        }


    }


}