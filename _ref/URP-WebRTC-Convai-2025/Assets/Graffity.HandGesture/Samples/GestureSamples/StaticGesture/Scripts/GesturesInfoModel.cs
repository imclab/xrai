using System;
using UnityEngine;
using R3;
using System.Collections.Generic;


namespace Graffity.HandGesture.Sample.StaticGesture
{


    [Serializable]
    public class GesturesInfoModel : IDisposable
    {


        [field: SerializeField]
        List<GestureModel> m_gestureModelList = new();

        public IReadOnlyList<GestureModel> GestureModelList => m_gestureModelList;

        readonly CompositeDisposable m_disposable = new();


        public void Initialize()
        {
            foreach (var gestureModel in m_gestureModelList)
            {
                gestureModel.Initialize();
            }
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
            foreach (var gestureModel in m_gestureModelList)
            {
                gestureModel.Dispose();
            }
        }


    }


}