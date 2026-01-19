using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlimeGun
{


    public class SlimeGunVFX : MonoBehaviour
    {

        public GameObject slimeSplashVFX;
        private GameObject slimeSplashVFXClone;

        public float shotReloadDelay = 0.01f;
        private bool fireDelay = false;



        private ParticleSystem slimeParticles;
        private ParticleSystem.Particle[] particles;

        void Start()
        {
            slimeParticles = GetComponent<ParticleSystem>();
            if (slimeParticles == null)
            {
                Debug.LogError("ParticleSystem component not found on this GameObject.");
                enabled = false;
                return;
            }

            particles = new ParticleSystem.Particle[GetComponent<ParticleSystem>().main.maxParticles];

            slimeSplashVFX.SetActive(false);

        }


        void LateUpdate()
        {

            if (Input.GetButtonDown("Fire1"))
            {

                if (fireDelay == false)
                {

                    StartCoroutine("FireGun");

                }


            }


            if (GetComponent<ParticleSystem>() != null)
            {
                int particleCount = GetComponent<ParticleSystem>().GetParticles(particles);

                for (int i = 0; i < particleCount; i++)
                {
                    if (particles[i].remainingLifetime <= 0)
                    {

                        Vector3 velocity = particles[i].velocity;

                        Quaternion targetRotation = Quaternion.LookRotation(velocity);

                        Vector3 position = particles[i].position;

                        slimeSplashVFXClone = Instantiate(slimeSplashVFX, position, targetRotation);

                        slimeSplashVFXClone.SetActive(true);

                        float anglex = slimeSplashVFXClone.transform.eulerAngles.x;
                        float angley = slimeSplashVFXClone.transform.eulerAngles.y;
                        float anglez = slimeSplashVFXClone.transform.eulerAngles.z;


                        // Tweak declal angle if too acute to avoid it getting cut off
                        anglex -= 360;
                        if (anglex > -50 && anglex < 1)
                        {
                            anglex = -51;
                        }
                        slimeSplashVFXClone.transform.eulerAngles = new Vector3(anglex, angley, anglez);



                    }
                }
            }

        }

        IEnumerator FireGun()
        {

            fireDelay = true;

            slimeParticles.Play();

            yield return new WaitForSeconds(shotReloadDelay);

            fireDelay = false;

        }


    }

}