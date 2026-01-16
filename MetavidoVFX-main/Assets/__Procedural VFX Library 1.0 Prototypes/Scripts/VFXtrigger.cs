using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VFXtrigger : MonoBehaviour
{

    public VisualEffect myEffect;
    public bool playit;
    // eventAttribute;
    // Start is called before the first frame update
    void Start()
    {


       

// As a field in the MonoBehaviour



myEffect = GetComponent<VisualEffect>();

      //  vfx = GetComponent<VisualEffect>();


    }

    // Update is called once per frame
    void Update()
    {
        if (playit == true)
        {
            myEffect.Play();
            // eventAttribute.SetVector3("position", target.position);
           // var eventAttribute = myEffect.CreateVFXEventAttribute();

            //myEffect.SendEvent("onPlay", eventAttribute);
        }
    }
}
