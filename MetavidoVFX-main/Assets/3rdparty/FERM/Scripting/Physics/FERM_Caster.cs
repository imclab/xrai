using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FERM_Caster {

    //transform
    private Vector3 shift;
    private Quaternion rot;
    private float scale;

    public FERM_Renderer parent;
    private List<FERM_CharacterizingComponent> baseComponents;

    //static parameter provision (bad coding practice but I couldn't be arsed to fix this for a single use case)
    public static Vector3 inputPos;

    //Default values
    private const float DEFAULT_MAX_DISTANCE = 1e+3f;

    //raymarch settings (inherited from renderer)
    public int maxSteps;
    public float overSample;
    public float stopDistance;

    public FERM_Caster(FERM_Renderer parent, List<FERM_CharacterizingComponent> baseComponents) {
        this.parent = parent;
        this.baseComponents = baseComponents;

        UpdateRaymarchSettings();
    }

    public void UpdateRaymarchSettings() {
        float qualityFactor = parent.material.GetFloat("_Qx");
        float cutoffFactor = parent.material.GetFloat("_Qy");
        float overSampleFactor = parent.material.GetFloat("_Qz");

        maxSteps = Mathf.CeilToInt(Mathf.Exp(2.302585f * (2.5f + .4f * qualityFactor + cutoffFactor + overSampleFactor)));
        stopDistance = Mathf.Exp(-2.302585f * (3f + qualityFactor));
        overSample = Mathf.Exp(-2.302585f * overSampleFactor);
    }

    public bool Raymarch(Ray ray) {
        FERM_RaymarchResult info;
        return Raymarch(ray.origin, ray.direction, out info, DEFAULT_MAX_DISTANCE);
    }

    public bool Raymarch(Ray ray, out FERM_RaymarchResult info) {
        return Raymarch(ray.origin, ray.direction, out info, DEFAULT_MAX_DISTANCE);
    }

    public bool Raymarch(Ray ray, float maxDistance) {
        FERM_RaymarchResult info;
        return Raymarch(ray.origin, ray.direction, out info, maxDistance);
    }

    public bool Raymarch(Ray ray, out FERM_RaymarchResult info, float maxDistance) {
        return Raymarch(ray.origin, ray.direction, out info, maxDistance);
    }

    public bool Raymarch(Vector3 origin, Vector3 direction) {
        FERM_RaymarchResult info;
        return Raymarch(origin, direction, out info, DEFAULT_MAX_DISTANCE);
    }

    public bool Raymarch(Vector3 origin, Vector3 direction, out FERM_RaymarchResult info) {
        return Raymarch(origin, direction, out info, DEFAULT_MAX_DISTANCE);
    }

    public bool Raymarch(Vector3 origin, Vector3 direction, float maxDistance) {
        FERM_RaymarchResult info;
        return Raymarch(origin, direction, out info, maxDistance);
    }

    public bool Raymarch(Vector3 origin, Vector3 direction, out FERM_RaymarchResult info, float maxDistance) {
        info = CustomMarch(origin, direction, maxDistance, stopDistance, true, maxSteps);
        return info.hit;
    }

    public FERM_RaymarchResult CustomMarch(Vector3 origin, Vector3 direction, float maxDistance, float stopDistance, bool scaleStop, int maxSteps) {
        FERM_RaymarchResult info = new FERM_RaymarchResult(this, maxDistance, stopDistance, scaleStop);
        direction = direction.normalized;
        info.SetRay(origin, direction);
        Vector3 point = origin;

        for(int i = 0; i < maxSteps; i++) {
            float d = Probe(point);
            if(info.AddStep(d))
                break;
            point += d * direction;
        }

        return info;
    }

    public FERM_RaymarchResult DefaultMarch(Ray ray) {
        return DefaultMarch(ray.origin, ray.direction);
    }

    public FERM_RaymarchResult DefaultMarch(Vector3 origin, Vector3 direction) {
        return CustomMarch(origin, direction, DEFAULT_MAX_DISTANCE, stopDistance, false, maxSteps);
    }

    public Vector3 GetNormal(Vector3 worldPosition) {
        return GetNormal(worldPosition, stopDistance);
    }

    public Vector3 GetNormal(Vector3 worldPosition, float d) {
        d /= 2f;
        Vector3 toReturn = new Vector3(
            Probe(worldPosition + d * Vector3.right) - Probe(worldPosition - d * Vector3.right),
            Probe(worldPosition + d * Vector3.up) - Probe(worldPosition - d * Vector3.up),
            Probe(worldPosition + d * Vector3.forward) - Probe(worldPosition - d * Vector3.forward)
        );
        return toReturn.normalized;
    }

    public float Probe(Vector3 worldPosition) {
        Vector3 inputPos = FERM_Modifier.PreTransform(worldPosition, shift, rot, scale);
        float value = DF(inputPos);
        return FERM_Modifier.PostTransform(value, shift, rot, scale);
    }

    private float DF(Vector3 pos) {
        //perform manual union operation on base components
        inputPos = pos;
        float min = float.PositiveInfinity;
        foreach(FERM_CharacterizingComponent c in baseComponents) {
            float d = c.DF(inputPos);
            if(d < min)
                min = d;
        }
        return min;
    }

    public void UpdateTransform(Vector3 shift, Quaternion rot, float scale) {
        this.shift = shift;
        this.rot = rot;
        this.scale = scale;
    }

}
