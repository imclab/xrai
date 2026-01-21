using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FERM_Example_CamLook : MonoBehaviour {

    Vector3 lastMousePos = Vector3.zero;
    public float sensitivity = 0.2f;

    private void Start() {
        lastMousePos = Input.mousePosition;
    }
    void Update () {
        Vector3 delta = lastMousePos - Input.mousePosition;
        Vector3 eulDelta = new Vector3(delta.y, -delta.x, 0f);
        lastMousePos = Input.mousePosition;
        transform.rotation *= Quaternion.Euler(sensitivity * eulDelta);
    }
}
