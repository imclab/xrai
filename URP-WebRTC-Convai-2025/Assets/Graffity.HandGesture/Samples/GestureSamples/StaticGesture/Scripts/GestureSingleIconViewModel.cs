using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.StaticGesture
{


    public class GestureSingleIconViewModel : GestureSingleIconView.IViewModel
    {


        public ReadOnlyReactiveProperty<bool> IsChangeResult => m_isChangeResult;
        public ReadOnlyReactiveProperty<string> GestureName => m_gestureName;
        public ReadOnlyReactiveProperty<Sprite> IconImage => m_iconImage;

        ReactiveProperty<bool> m_isChangeResult = new();
        ReactiveProperty<string> m_gestureName = new();
        ReactiveProperty<Sprite> m_iconImage = new();
        readonly CompositeDisposable m_disposable = new();


        public GestureSingleIconViewModel()
        {
            m_isChangeResult.AddTo(m_disposable);
            m_gestureName.AddTo(m_disposable);
            m_iconImage.AddTo(m_disposable);
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
        }


        public void Bind(Gesture gesture)
        {
            m_isChangeResult.Value = gesture.Result;
            gesture.OnChangeResult()
                .Subscribe(value => m_isChangeResult.Value = value.result)
                .AddTo(m_disposable);
        }

        public void Bind(GestureModel gestureModel)
        {
            m_gestureName.Value = gestureModel.ID.CurrentValue;
            m_iconImage.Value = gestureModel.Icon1.CurrentValue;
            gestureModel.ID
                .Subscribe(value => m_gestureName.Value = value)
                .AddTo(m_disposable);
            gestureModel.Icon1
                .Subscribe(value => m_iconImage.Value = value)
                .AddTo(m_disposable);
        }


    }


}