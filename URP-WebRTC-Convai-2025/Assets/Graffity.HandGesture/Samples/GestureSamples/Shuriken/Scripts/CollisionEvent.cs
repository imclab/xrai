using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Shuriken
{


    public class CollisionEvent : MonoBehaviour
    {


        Subject<Collision> m_collisionEnter = new();
        public Observable<Collision> CollisionEnter => m_collisionEnter;


        void Awake()
        {
            m_collisionEnter.RegisterTo(this.destroyCancellationToken);
        }


        void OnCollisionEnter(Collision collision)
        {
            m_collisionEnter.OnNext(collision);
        }


    }


}