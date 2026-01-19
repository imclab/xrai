using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.FingerGun
{


    public class BulletModel
    {


        public ReadOnlyReactiveProperty<Vector3> Position => m_position;
        public ReadOnlyReactiveProperty<Vector3> MoveVec => m_moveVec;
        public ReadOnlyReactiveProperty<bool> IsActive => m_isActive;

        ReactiveProperty<Vector3> m_position = new();
        ReactiveProperty<Vector3> m_moveVec = new();
        ReactiveProperty<bool> m_isActive = new(false);

        const float SHOT_SPEED = 0.1f;


        public void Shot(Vector3 position, Vector3 moveVec)
        {
            m_isActive.Value = true;
            m_position.Value = position;
            m_moveVec.Value = moveVec.normalized * SHOT_SPEED;
        }

        public void Update()
        {
            if (!m_isActive.Value) return;
            m_position.Value += m_moveVec.Value;
        }

        public void HitTarget(Collision collision)
        {
            if (collision.collider.tag == "Target")
            {
                m_isActive.Value = false;
            }
        }


    }


}