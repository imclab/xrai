using UnityEngine;

public class FERM_Example_CastProbe : MonoBehaviour {

    new private FERM_Renderer renderer;
    public Transform probe;

    private void Update() {
        FERM_RaymarchResult result;
        if(renderer == null)
            renderer = FindObjectOfType<FERM_Renderer>();
        renderer.GetCaster().Raymarch(transform.position, transform.forward, out result);
        probe.position = result.point;
        probe.LookAt(result.point + result.normal);
    }
}
