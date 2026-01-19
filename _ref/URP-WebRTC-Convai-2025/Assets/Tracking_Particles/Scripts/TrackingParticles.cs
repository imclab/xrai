using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace tracking
{

    public class TrackingParticles : MonoBehaviour
    {
        ParticleSystem pSystem;
        Particle[] particles;

        [SerializeField] Transform target;
        [SerializeField] float speed = 1f;
        [SerializeField] bool targetEnable = true;

        private void Start()
        {
            if (pSystem == null)
            {
                pSystem = GetComponent<ParticleSystem>();
                particles = new Particle[pSystem.main.maxParticles];
            }
        }

        private void Update()
        {
            if (targetEnable && target) Test_Particle();
        }

        void Test_Particle()
        {
            int length = pSystem.GetParticles(particles);

            for (int i = 0; i < length; i++)
            {
                Vector3 posA = particles[i].position;
                Vector3 posB = target.position;

                if (Vector3.Distance(posA, posB) > 0.1f)
                {
                    float variableSpeed = (pSystem.main.startSpeedMultiplier / (particles[i].remainingLifetime + 0.1f)) + particles[i].startLifetime;
                    particles[i].position = Vector3.MoveTowards(posA, posB, variableSpeed * Time.deltaTime * speed);
                }
                else particles[i].remainingLifetime = -0.1f;
            }

            pSystem.SetParticles(particles, length);
        }
    }

}