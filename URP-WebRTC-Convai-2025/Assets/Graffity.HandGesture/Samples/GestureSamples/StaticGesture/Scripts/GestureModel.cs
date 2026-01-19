using System;
using UnityEngine;
using R3;


namespace Graffity.HandGesture.Sample.StaticGesture
{


    [Serializable]
    public class GestureModel : IDisposable
    {


        [field: SerializeField]
        string m_id;
        [field: SerializeField]
        Sprite m_icon1;
        [field: SerializeField]
        Sprite m_icon2;

        public ReadOnlyReactiveProperty<string> ID => ReactivePropertyID;
        public ReadOnlyReactiveProperty<Sprite> Icon1 => ReactivePropertyIcon1;
        public ReadOnlyReactiveProperty<Sprite> Icon2 => ReactivePropertyIcon2;

        ReactiveProperty<string> ReactivePropertyID { get; set; } = new();
        ReactiveProperty<Sprite> ReactivePropertyIcon1 { get; set; } = new();
        ReactiveProperty<Sprite> ReactivePropertyIcon2 { get; set; } = new();
        readonly CompositeDisposable m_disposable = new();


        public void Initialize()
        {
            ReactivePropertyID.Value = m_id;
            ReactivePropertyIcon1.Value = m_icon1;
            ReactivePropertyIcon2.Value = m_icon2;
            ReactivePropertyID.AddTo(m_disposable);
            ReactivePropertyIcon1.AddTo(m_disposable);
            ReactivePropertyIcon2.AddTo(m_disposable);
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
        }


    }


}