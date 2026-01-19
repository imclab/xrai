using UnityEngine;

[ExecuteAlways]
public class ObjectMotionFeed : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedRenderer;
    public Material targetMaterial;

    private Vector3 lastCenter;
    private Vector3 velocity;

    void Start()
    {
        if (skinnedRenderer == null)
            skinnedRenderer = GetComponent<SkinnedMeshRenderer>();

        if (skinnedRenderer != null)
            lastCenter = skinnedRenderer.bounds.center;

        velocity = Vector3.zero;    
    }

    void Update()
    {
        if (skinnedRenderer == null || targetMaterial == null)
            return;

        Vector3 currentCenter = skinnedRenderer.bounds.center;
        velocity = (currentCenter - lastCenter) / Time.deltaTime;
        lastCenter = currentCenter;

        targetMaterial.SetVector("_SurfaceNoiseScroll", velocity);
        //Debug.Log(velocity);
    }
}
