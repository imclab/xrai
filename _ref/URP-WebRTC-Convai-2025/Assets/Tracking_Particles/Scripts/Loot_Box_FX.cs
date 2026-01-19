using UnityEngine;
using System.Collections;

namespace LootBox
{


    public class Loot_Box_FX : MonoBehaviour
    {


        // public GameObject lootBox;
        public GameObject lootBoxAnim;
        public GameObject lootBoxFX;
        public GameObject godRays;
        public GameObject buildupGlow;

        private float fadeStart = 5;
        private float fadeEnd = 0;
        private float fadeTime = 1.8f;
        private float t = 0.0f;

        private bool boxOpened = false;

        void Start()
        {

            lootBoxFX.SetActive(false);
            godRays.SetActive(false);
            buildupGlow.SetActive(false);

        }


        void Update()
        {

            if (Input.GetButtonDown("Fire1"))
            {

                if (boxOpened == false)
                {

                    StartCoroutine("OpenBox");

                }
            }

        }



        IEnumerator OpenBox()
        {


            lootBoxAnim.GetComponent<Animation>().Play();

            lootBoxFX.SetActive(true);

            buildupGlow.SetActive(true);

            yield return new WaitForSeconds(1.55f);

            godRays.SetActive(true);

            boxOpened = true;

        }


    }

}
