using UnityEngine;

public class FERM_RaymarchResult {

    //settings
    public readonly FERM_Caster parent;
    public readonly float maxDistance;
    public readonly float minDistance;
    public readonly bool scaleStopConditionWithLength;

    //ray info
    private Vector3 startPoint, direction;

    //general results
    public bool hit { get; private set; }
    public bool cutoff { get; private set; }
    public float totalLength { get; private set; }
    public int totalSteps { get; private set; }

    //hit specific results
    public Vector3 point { get; private set; }
    public Vector3 normal { get; private set; }
    public float distance { get; private set; }

    //mis specific results
    public float shortestDistance { get; private set; }

    public FERM_RaymarchResult(FERM_Caster parent, float maxDistance, float minDistance, bool scaleStop) {
        this.parent = parent;
        this.maxDistance = maxDistance;
        this.minDistance = minDistance;
        this.scaleStopConditionWithLength = scaleStop;

        hit = false;
        totalLength = 0f;
        shortestDistance = float.PositiveInfinity;
        totalSteps = 0;
        cutoff = true;
    }

    public void SetRay(Vector3 position, Vector3 direction) {
        this.startPoint = position;
        this.direction = direction;

        point = this.startPoint;
        normal = -this.direction;
    }

    public bool AddStep(float d) {
        totalLength += d;
        if(d < shortestDistance)
            shortestDistance = d;
        totalSteps++;

        float stopDistance = minDistance;
        if(scaleStopConditionWithLength)
            stopDistance *= totalLength;

        hit = d < stopDistance;

        if(hit) {
            distance = totalLength;
            point = startPoint + distance * direction;
            Vector3 normalPoint = point - d * direction;
            normal = parent.GetNormal(normalPoint, stopDistance);
            cutoff = false;
            return true;
        }

        bool mis = totalLength > maxDistance;
        if(mis) {
            cutoff = false;
            return true;
        }

        return false;
    }
}
