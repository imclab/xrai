using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FERM_Example_CamZoom : MonoBehaviour {

    public float sensitivity = 0.01f;
    
    void Update () {
        Camera cam = GetComponent<Camera>();
        if(cam == null)
            return;

        float delta = -Input.mouseScrollDelta.y;

        cam.fieldOfView *= Mathf.Exp(sensitivity * delta);
    }
}
