using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Beam
{


    public class TriggerEvent : MonoBehaviour
    {


        Subject<Collider> m_triggerStay = new();
        public Observable<Collider> TriggerStay => m_triggerStay;


        void Awake()
        {
            m_triggerStay.RegisterTo(this.destroyCancellationToken);
        }


        void OnTriggerStay(Collider collider)
        {
            m_triggerStay.OnNext(collider);
        }


    }


}