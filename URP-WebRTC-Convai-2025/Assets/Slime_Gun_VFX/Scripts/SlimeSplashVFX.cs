using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SlimeGun
{


    public class SlimeSplashVFX : MonoBehaviour
    {

        public GameObject splatDecal_01;
        public GameObject splatDecal_02;
        public GameObject splatDecal_03;
        public GameObject splatDecal_04;
        public GameObject smallDripsDecal;

        public float splatDecalMinSize = 0.6f;
        public float splatDecalMaxSize = 1.2f;
        public int decalLifetime = 60;

        public GameObject alembicLiquid;

        public GameObject VFX;

        public AudioSource splatAudio01;
        public AudioSource splatAudio02;
        public AudioSource splatAudio03;
        public AudioSource splatAudio04;
        public AudioSource splatAudio05;

        private int rndSplat;
        private float rndSplatScale;
        private Vector3 splatTargetScale;
        private Quaternion decalTargetRotation;
        private float duration = 0.25f; // The time it takes to scale up decal on impact (in seconds)
        private Vector3 initialScale;
        private float startTime;
        private int splatAudioNum;
        private bool scaling = false;


        // Start is called before the first frame update
        void Start()
        {

            VFX.SetActive(true);

            splatDecal_01.SetActive(false);
            splatDecal_02.SetActive(false);
            splatDecal_03.SetActive(false);
            splatDecal_04.SetActive(false);
            smallDripsDecal.SetActive(false);

            rndSplat = Random.Range(1, 5);

            rndSplatScale = Random.Range(splatDecalMinSize, splatDecalMaxSize);

            splatTargetScale = new Vector3(rndSplatScale, rndSplatScale, rndSplatScale);

            splatDecal_01.transform.localScale = splatTargetScale;
            splatDecal_02.transform.localScale = splatTargetScale;
            splatDecal_03.transform.localScale = splatTargetScale;
            splatDecal_04.transform.localScale = splatTargetScale;
            smallDripsDecal.transform.localScale = splatTargetScale;

            splatDecal_01.SetActive(false);
            splatDecal_02.SetActive(false);
            splatDecal_03.SetActive(false);
            splatDecal_04.SetActive(false);

            splatDecal_01.transform.Rotate(0, 0, (Random.Range(0, 360)));
            splatDecal_02.transform.Rotate(0, 0, (Random.Range(0, 360)));
            splatDecal_03.transform.Rotate(0, 0, (Random.Range(0, 360)));
            splatDecal_04.transform.Rotate(0, 0, (Random.Range(0, 360)));

            alembicLiquid.transform.Rotate(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));

            if (rndSplat == 1) { splatDecal_01.SetActive(true); }
            if (rndSplat == 2) { splatDecal_02.SetActive(true); }
            if (rndSplat == 3) { splatDecal_03.SetActive(true); }
            if (rndSplat == 4) { splatDecal_04.SetActive(true); }

            SplatSounds();

            StartScaling();

            StartScalingToZero();

        }

        // Update is called once per frame
        void Update()
        {

            if (scaling)
            {
                PerformScaling(); // Call the void function
            }

        }


        public void StartScaling()
        {
            startTime = Time.time;
            scaling = true;
        }

        public void SplatSounds()
        {

            splatAudioNum = Random.Range(1, 6);

            if (splatAudioNum == 1) { splatAudio01.Play(); }
            if (splatAudioNum == 2) { splatAudio02.Play(); }
            if (splatAudioNum == 3) { splatAudio03.Play(); }
            if (splatAudioNum == 4) { splatAudio04.Play(); }
            if (splatAudioNum == 5) { splatAudio05.Play(); }

        }



        private void PerformScaling()
        {

            float timeElapsed = Time.time - startTime;
            float progress = Mathf.Clamp01(timeElapsed / duration);

            splatDecal_01.transform.localScale = Vector3.Lerp(Vector3.zero, splatTargetScale, progress);
            splatDecal_02.transform.localScale = Vector3.Lerp(Vector3.zero, splatTargetScale, progress);
            splatDecal_03.transform.localScale = Vector3.Lerp(Vector3.zero, splatTargetScale, progress);
            splatDecal_04.transform.localScale = Vector3.Lerp(Vector3.zero, splatTargetScale, progress);

            if (progress >= 1f)
            {

                scaling = false;

            }

        }


        public void StartScalingToZero()
        {
            StartCoroutine(ScaleDownOverTime());
        }

        private IEnumerator ScaleDownOverTime()
        {

            yield return new WaitForSeconds(0.3f);

            smallDripsDecal.transform.Rotate(0, 0, (Random.Range(0, 360))); ;

            smallDripsDecal.SetActive(true);

            yield return new WaitForSeconds(2f);

            float timeElapsed = 0f;

            while (timeElapsed < decalLifetime)
            {
                float progress = timeElapsed / decalLifetime;
                splatDecal_01.transform.localScale = Vector3.Lerp(splatTargetScale, Vector3.zero, progress);
                splatDecal_02.transform.localScale = Vector3.Lerp(splatTargetScale, Vector3.zero, progress);
                splatDecal_03.transform.localScale = Vector3.Lerp(splatTargetScale, Vector3.zero, progress);
                splatDecal_04.transform.localScale = Vector3.Lerp(splatTargetScale, Vector3.zero, progress);
                smallDripsDecal.transform.localScale = Vector3.Lerp(splatTargetScale, Vector3.zero, progress);

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            splatDecal_01.transform.localScale = Vector3.zero; // Ensure final scale is exactly zero

            Destroy(this.gameObject);
        }


    }

}
