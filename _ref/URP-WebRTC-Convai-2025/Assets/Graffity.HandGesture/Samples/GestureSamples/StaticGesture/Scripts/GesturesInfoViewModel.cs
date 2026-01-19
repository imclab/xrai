using System;
using System.Collections.Generic;
using R3;


namespace Graffity.HandGesture.Sample.StaticGesture
{


    public class GesturesInfoViewModel : GesturesInfoView.IViewModel, IDisposable
    {


        public ReadOnlyReactiveProperty<string> CurrentGesture => m_currentGesture;
        public IReadOnlyList<IGestureIconViewModel> GestureIconViewModelList => m_gestureIconViewModelList;

        ReactiveProperty<string> m_currentGesture = new();
        List<IGestureIconViewModel> m_gestureIconViewModelList = new();
        readonly CompositeDisposable m_disposable = new();


        public GesturesInfoViewModel()
        {
            m_currentGesture.AddTo(m_disposable);
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
            foreach (var gestureIcon in m_gestureIconViewModelList)
            {
                gestureIcon.Dispose();
            }
        }


        public void Bind(GestureManager gestureManager, GesturesInfoModel gesturesInfoModel)
        {
            foreach (var gestureModel in gesturesInfoModel.GestureModelList)
            {
                IGestureIconViewModel gestureIcon = gestureModel.Icon2.CurrentValue == null
                    ? new GestureSingleIconViewModel()
                    : new GestureDoubleIconViewModel();
                gestureIcon.Bind(gestureModel);
                m_gestureIconViewModelList.Add(gestureIcon);
                if (gestureManager.TryGetGesture(gestureModel.ID.CurrentValue, out var gesture))
                {
                    gestureIcon.Bind(gesture);
                    gesture.OnChangeResult()
                        .Where(value => value.result)
                        .Subscribe(value => m_currentGesture.Value = gestureModel.ID.CurrentValue)
                        .AddTo(m_disposable);
                    gesture.OnChangeResult()
                        .Where(value => !value.result && m_currentGesture.Value == gestureModel.ID.CurrentValue)
                        .Subscribe(value => m_currentGesture.Value = string.Empty)
                        .AddTo(m_disposable);
                }
            }
        }


    }


}