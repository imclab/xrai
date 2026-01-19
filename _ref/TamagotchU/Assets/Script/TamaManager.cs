using AfterimageSample;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics.Geometry;
using Unity.MLAgents.Integrations.Match3;
using Unity.VisualScripting;
using UnityEngine;

public struct TamaEmo
{
    public float calm;
    public float hyped;
    public float lovey;
    public float alarming;
    public float annoyned;
}

public struct TransformData
{
    public Vector3 position;
    public Vector3 forward;

    public TransformData(Vector3 pos, Vector3 fwd)
    {
        position = pos;
        forward = fwd;
    }
}

public class TamaManager : MonoBehaviour
{
    [SerializeField] SocketReceiver socketReceiver;
    [SerializeField] FrameRequester frameRequester;
    [SerializeField] GameObject bubble;
    [SerializeField] GameObject tamaBg;
    [SerializeField] SkinnedMeshRenderer tamaRenderer;
    [SerializeField] GameObject[] alarms;
    [Header("Audio Source")]
    [SerializeField] AudioSource calmAudio;
    [SerializeField] AudioSource hypeAudio;
    [SerializeField] AudioSource posAudio;
    [SerializeField] AudioSource negAudio;
    [SerializeField] AudioSource alarmAudio;
    [Header("Emotion Emulator")]
    [SerializeField] bool debugMode = false;
    [SerializeField][Range(0, 1)] float hypeDebug;
    [SerializeField][Range(0, 1)] float posDebug;
    [SerializeField][Range(0, 1)] float negDebug;
    [SerializeField][Range(0, 1)] float alarmingDebug;


    public TamaEmo tamaEmo;
    private Coroutine happyMouthBlendShape;
    int mouthShapekeyIndex = 0;
    int bodyShapekeyIndex = 1;

    float GetAnimateValue(float val)
    {
        return Mathf.Sin(Time.fixedTime * Mathf.Rad2Deg * val) * 0.5f + 0.5f; // 0-1
    }
    private void Start()
    {
        calmAudio.loop = true;
        calmAudio.Play();
        hypeAudio.loop = true;
        hypeAudio.Play();
        posAudio.loop = true;
        posAudio.Play(); 
        negAudio.loop = true; 
        negAudio.Play();
        alarmAudio.loop = true;
        alarmAudio.Play();
    }
    private void Update()
    {
        ProcessTamaEmo();

        float hypeVal = debugMode ? hypeDebug : tamaEmo.hyped;
        float calmVal = 1 - hypeVal;
        float posVal = debugMode ? posDebug : tamaEmo.lovey;
        float alarmVal = debugMode ? alarmingDebug : tamaEmo.alarming;
        float negVal = debugMode ? negDebug : tamaEmo.annoyned;

        float bodyLow = 0, bodyHigh = 100, bodyLerp = negVal;
        float mouthLow = 0, mouthHigh = 100, mouthLerp = (posVal + negVal) /2.0f;

        // calm <-----> hype
        TransformData bounceTrans = BounceMotion(transform);
        TransformData SpinTrans = SpinMotion(transform, sphereCenter);
        transform.position = Vector3.Lerp(bounceTrans.position, SpinTrans.position, hypeVal);
        transform.forward = Vector3.Lerp(bounceTrans.forward, SpinTrans.forward, hypeVal);
        if (bubble)
        {
            float scale = Mathf.Lerp(0.15f, 1.0f, calmVal);
            bubble.transform.localScale = new Vector3(scale, scale, scale);
            bubble.transform.position = transform.position + new Vector3(0, 0, -0.5f * hypeVal);
        }
        if(GetComponent<AfterimageRenderer>() != null)
        {
            GetComponent<AfterimageRenderer>().Duration = (int)Mathf.Lerp(1, 125, hypeVal);
        }
        calmAudio.volume = calmVal;
        hypeAudio.volume = hypeVal * 0.85f;
        hypeAudio.pitch = hypeVal * 1;

        // happy
        if(posVal > 0.56)
        {
            mouthHigh = 100 * posVal;
            mouthLow = 0;
            if (!debugMode) frameRequester.HumanBorn(transform.position); // spawn human fish unless debug mode
            //ChangeMouthShape(100, 0, 0.25f); // close mouth
        }
        posAudio.volume = Mathf.Pow(posVal, 4);
        posAudio.pitch = Mathf.Pow(posVal * 2, 2);

        // alarm
        for (int i = 0; i < alarms.Length; i++)
        {
            if (alarms[i])
            {
                // alarm material
                float alarmEmi = (Mathf.Sin(Mathf.Rad2Deg * Time.fixedTime) + 1) * 10 * alarmVal;
                alarms[i].GetComponent<MeshRenderer>().material.SetFloat("_Emission", Mathf.Lerp(1, alarmEmi, alarmVal));
            }
        }
        Material skyboxMat = RenderSettings.skybox;
        if (skyboxMat)
        {
            skyboxMat.SetFloat("_Speed", Mathf.Lerp(-0.1f, 0.45f, alarmVal));
            skyboxMat.SetFloat("_LCDScale", Mathf.Lerp(65.0f, 1.0f, alarmVal));
            skyboxMat.SetFloat("_LEDScale", Mathf.Lerp(5.0f, 95.0f, alarmVal));
            skyboxMat.SetFloat("_VoronoiScale", Mathf.Lerp(7.0f, 0.0f, alarmVal));
        }
        alarmAudio.volume = alarmVal;
        alarmAudio.pitch = alarmVal * 2;

        // neg: shapekeys, material
        mouthLow = -50 * negVal;
        bodyLow = 100 * negVal;
        negAudio.volume = negVal;

        // shapekeys
        bodyLerp = negVal;
        tamaRenderer.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(bodyShapekeyIndex, Mathf.Lerp(bodyLow, bodyHigh, bodyLerp));
        tamaBg.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(bodyShapekeyIndex, Mathf.Lerp(bodyLow, bodyHigh, bodyLerp));
        mouthLerp = GetAnimateValue(posVal + negVal);
        tamaRenderer.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(mouthShapekeyIndex, Mathf.Lerp(mouthLow, mouthHigh, mouthLerp));
        tamaBg.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(mouthShapekeyIndex, Mathf.Lerp(mouthLow, mouthHigh, posVal - negVal));

        //Debug.Log(DebugEmo());
    }

    void ProcessTamaEmo()
    {
        int playerEmoCnt = socketReceiver.PlayerEmoEntries.Count;

        float happyWeightedSum = 0, negWeightedSum = 0;
        float totalPosWeight = 1, totalNegWeight = 1;
        List<string> emoTagList = new List<string>();

        // Weights: oldest = 1, newest = count (simple linear scale)
        for (int i = 0; i < playerEmoCnt; i++)
        {
            string emoTag = socketReceiver.PlayerEmoEntries[i].message;
            float emoVal = socketReceiver.PlayerEmoEntries[i].value;
            if (!emoTagList.Contains(emoTag))
                emoTagList.Add(emoTag);

            float weight = i + 1.25f;
            if (emoTag == "Happiness")
            {
                happyWeightedSum += emoVal * weight;
                totalPosWeight += weight;
            }
            else if (emoTag == "Sadness" || emoTag == "Fear" | emoTag == "Disgust" | emoTag == "Anger" | emoTag == "Surprise")
            {
                negWeightedSum += emoVal * weight;
                totalNegWeight += weight;
            }
        }

        // normalize weighted average
        happyWeightedSum /= totalPosWeight;
        negWeightedSum /= totalNegWeight;
        tamaEmo.lovey = happyWeightedSum;
        tamaEmo.annoyned = Mathf.Pow(negWeightedSum, 0.8f);
        tamaEmo.alarming = Mathf.Pow(emoTagList.Count / 7.0f, 0.45f); // 7 types of emotion in total
        tamaEmo.hyped = Mathf.Clamp01(playerEmoCnt / 5f);
        tamaEmo.calm = 1.0f - tamaEmo.hyped;
    }

    [Header("Bounce Motion")]
    public Vector3 sphereCenter = Vector3.zero;
    public float sphereRadius = 5f;
    public Vector3 velocity = new Vector3(1, 2, 1.5f);
    public float damping = 0.98f; // slows it down a bit each bounce
    public float accelerationStrength = 0f; // set >0 for gravity, e.g., 9.8f
    private TransformData BounceMotion(Transform trans)
    {
        Vector3 acceleration = Vector3.zero;
        // Uncomment the next line for downward gravity:
        // acceleration = Vector3.down * accelerationStrength;
        // Or center-seeking gravity:
        // acceleration = (sphereCenter - transform.position).normalized * accelerationStrength;
        TransformData t = new TransformData(trans.position, trans.forward);
        Vector3 pos = trans.position;
        velocity += acceleration * Time.deltaTime;
        pos += velocity * Time.deltaTime;

        Vector3 toCenter = pos - sphereCenter;
        if (toCenter.sqrMagnitude > sphereRadius * sphereRadius)
        {
            toCenter = toCenter.normalized * sphereRadius;
            pos = sphereCenter + toCenter;

            Vector3 normal = toCenter.normalized;
            velocity = Vector3.Reflect(velocity, normal);
            velocity *= damping;
            velocity += Random.insideUnitSphere * 0.5f;
        }
        t.position = pos;
        // Align mesh forward direction to velocity if velocity is non-zero
        if (velocity.sqrMagnitude > 0.0001f)
        {
            t.forward = -velocity.normalized;
        }

        return t;
    }

    [Header("Spin Motion")]
    // Axis around which the mesh spins
    public Vector3 spinAxis = Vector3.up;
    // Rotation speed in degrees per second
    public float spinSpeed = 90f;
    TransformData SpinMotion(Transform trans, Vector3 center)
    {
        float angle = spinSpeed * Time.deltaTime;
        Vector3 axis = spinAxis.normalized;
        // Get vector from center of rotation to current position
        //Vector3 offset = new Vector3(Mathf.Sin(Time.fixedTime), Mathf.Cos(Time.fixedTime), 0) * 1.5f - center;
        Vector3 offset = trans.position - center;
        // Create a quaternion representing the rotation around the axis by the angle
        Quaternion rotation = Quaternion.AngleAxis(angle, axis);
        // Rotate the offset
        Vector3 rotatedOffset = rotation * offset;
        Vector3 newPosition = center + rotatedOffset;
        TransformData t = new TransformData(trans.position, trans.forward);
        t.position = newPosition;
        t.forward = Vector3.forward;
        return t;
    }

    private void ChangeMouthShape(float startValue, float endValue, float duration)
    {
        if (happyMouthBlendShape != null)
        {
            StopCoroutine(happyMouthBlendShape);
        }
        happyMouthBlendShape = StartCoroutine(ChangeMouthBlendShape(startValue, endValue, duration));
    }

    private IEnumerator ChangeMouthBlendShape(float startValue, float endValue, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Interpolate the blend shape value
            //float currentValue = Mathf.Lerp(startValue, endValue, t);
            float currentValue = Mathf.Sin(Mathf.Rad2Deg * elapsedTime) * 0.5f + 0.5f;
            currentValue = currentValue * Mathf.Abs(startValue - endValue) + startValue;
            tamaRenderer.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(mouthShapekeyIndex, currentValue);

            yield return null;
        }

        // Ensure the final value is set
        tamaRenderer.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(mouthShapekeyIndex, 0);
    }

    public string DebugEmo()
    {
        return $"Calm: {tamaEmo.calm}, Hyped: {tamaEmo.hyped}, Lovey: {tamaEmo.lovey}, Alarming: {tamaEmo.alarming}, Annoyned: {tamaEmo.annoyned}";
    }
}
