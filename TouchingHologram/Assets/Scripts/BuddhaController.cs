using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX.Utility;
using UnityEngine.VFX;

namespace HoloInteractive.CourseSample.InteractWithBuddha
{
    public class BuddhaController : MonoBehaviour
    {
        [SerializeField] VisualEffect vfx;

        // Start is called before the first frame update
        void Start()
        {
            var hand = FindObjectOfType<BuddhaSceneManager>().Hand;
            if (!hand)
            {
                Debug.Log("No HandObject Assigned!");
            }
            CustomVFXPositionBinder myPositionBinder = vfx.GetComponent<CustomVFXPositionBinder>();
            myPositionBinder.Target = hand.transform;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
