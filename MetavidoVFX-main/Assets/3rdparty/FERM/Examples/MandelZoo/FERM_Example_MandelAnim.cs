using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FERM_Example_MandelAnim : MonoBehaviour {

    private Material mat;

    private void Start() {
        mat = GetComponent<FERM_Renderer>().material;
        mat.mainTextureOffset = new Vector2(0f, 0.3f);
    }

    private void Update () {
        mat.mainTextureOffset += Time.deltaTime * new Vector2(0.1f, 0.01f);
	}
}
