using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.FingerGun
{


    public class TargetModel
    {


        public ReadOnlyReactiveProperty<Vector3> Position => m_position;

        ReactiveProperty<Vector3> m_position = new();

        static readonly Vector2 TARGET_SPAWN_X_RANGE = new(-1f, 1f);
        static readonly Vector2 TARGET_SPAWN_Y_RANGE = new(1f, 2f);
        static readonly Vector2 TARGET_SPAWN_Z_RANGE = new(2.5f, 3f);


        public TargetModel()
        {
            InitializePosition();
        }

        public void HitBullet(Collision collision)
        {
            if (collision.collider.tag == "Bullet")
            {
                InitializePosition();
            }
        }

        void InitializePosition()
        {
            m_position.Value = new Vector3(
                UnityEngine.Random.Range(TARGET_SPAWN_X_RANGE.x, TARGET_SPAWN_X_RANGE.y),
                UnityEngine.Random.Range(TARGET_SPAWN_Y_RANGE.x, TARGET_SPAWN_Y_RANGE.y),
                UnityEngine.Random.Range(TARGET_SPAWN_Z_RANGE.x, TARGET_SPAWN_Z_RANGE.y));
        }


    }


}