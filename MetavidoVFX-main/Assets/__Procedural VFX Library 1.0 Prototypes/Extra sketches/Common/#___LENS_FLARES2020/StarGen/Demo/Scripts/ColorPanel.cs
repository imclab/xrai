using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ColorPanel : MonoBehaviour
{
    Material mat;
    public Color col;
    // Use this for initialization
    void Start()
    {
        mat = GetComponent<Image>().material;
        col = new Color(0, 0, 0, 1f);
    }

    // Update is called once per frame
    public void OnBChange(float t)
    {
        col.b = t;
        mat.SetColor("_Color",col);
    }
    public void OnGChange(float t)
    {
        col.g = t;
        mat.SetColor("_Color", col);
    }
    public void OnRChange(float t)
    {
        col.r = t;
        mat.SetColor("_Color", col);
    }
}
