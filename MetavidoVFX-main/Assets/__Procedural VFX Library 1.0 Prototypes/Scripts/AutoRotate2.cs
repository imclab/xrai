using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRotate2 : MonoBehaviour
{
    public float Speed = 5.0f;
    public float dir = 1.0f;

    public bool RotateOnX = true;
    public bool RotateOnY = true;
    public bool RotateOnZ = true;
    public GameObject go;
	
	void Update ()
    {
        Vector3 rotFactor = Vector3.one * Speed;

        if (!RotateOnX) rotFactor.x = 0;
        if (!RotateOnY) rotFactor.y = 0;
        if (!RotateOnZ) rotFactor.z = 0;

        go.transform.Rotate(
            rotFactor * Time.deltaTime * dir
       );
    }
}
