using UnityEngine;
using UnityEngine.UI;

public class FERM_Example_PointProbe : MonoBehaviour {

    new private FERM_Renderer renderer;
    public Text output;

    private void Update() {
        if(renderer == null)
            renderer = FindObjectOfType<FERM_Renderer>();
        output.text = renderer.GetCaster().Probe(transform.position).ToString("F3");
    }
}
