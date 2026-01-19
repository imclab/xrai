using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.StaticGesture
{


    public class GestureDoubleIconViewModel : GestureDoubleIconView.IViewModel
    {


        public ReadOnlyReactiveProperty<bool> IsChangeResult => m_isChangeResult;
        public ReadOnlyReactiveProperty<string> GestureName => m_gestureName;
        public ReadOnlyReactiveProperty<Sprite> Icon1Image => m_icon1Image;
        public ReadOnlyReactiveProperty<Sprite> Icon2Image => m_icon2Image;

        ReactiveProperty<bool> m_isChangeResult = new();
        ReactiveProperty<string> m_gestureName = new();
        ReactiveProperty<Sprite> m_icon1Image = new();
        ReactiveProperty<Sprite> m_icon2Image = new();
        readonly CompositeDisposable m_disposable = new();


        public GestureDoubleIconViewModel()
        {
            m_isChangeResult.AddTo(m_disposable);
            m_gestureName.AddTo(m_disposable);
            m_icon1Image.AddTo(m_disposable);
            m_icon2Image.AddTo(m_disposable);
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
            m_icon1Image.Value = gestureModel.Icon1.CurrentValue;
            m_icon2Image.Value = gestureModel.Icon2.CurrentValue;
            gestureModel.ID
                .Subscribe(value => m_gestureName.Value = value)
                .AddTo(m_disposable);
            gestureModel.Icon1
                .Subscribe(value => m_icon1Image.Value = value)
                .AddTo(m_disposable);
            gestureModel.Icon2
                .Subscribe(value => m_icon2Image.Value = value)
                .AddTo(m_disposable);
        }


    }


}