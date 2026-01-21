using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class FERM_ParamAudio : FERM_ParamAccess {

    public AudioSource input;
    [Range(0, SAMPLES - 1)]
    public int inputFrequency;
    private const int SAMPLES = 128;
    private const int CHANNEL = 0;
    private const int SPEC_SAMPLES = 512;
    private float[] spectrum = new float[SAMPLES];

    [Range(0f, 100f)]
    public float thresholdLevel = 50f;
    public float minTime = .2f, attack = .05f, sustain = 1f;
    private float prevAudioValue, audioValue, timer;

    private Queue<float> ampSpectrum;

    public OuputType outputType;
    public enum OuputType {
        Direct, Derivative, Integrate
    }

    private float value;

    public float rest_float = 0f, beat_float = 1f;
    public Vector3 rest_vec = Vector3.zero, beat_vec = Vector3.up;
    public Quaternion rest_quat = Quaternion.identity, 
        beat_quat = Quaternion.Euler(0f, 90f, 0f);

    public FERM_Renderer rend { get { return GetComponentInParent<FERM_Renderer>(); } }

    private void Start() {
        value = 0f;
        timer = attack + sustain + minTime;
        ampSpectrum = new Queue<float>();
    }

    private void LateUpdate() {
        float input = GetInput();
        TrackInput(input);
        float beat = BeatUpdate(input);
        float t = UpdateValue(beat);
        object output = LerpTargetValue(t);
        if(outputType == OuputType.Integrate)
            output = AddTargetValue(GetTargetValue(), output);
        SetTargetValue(output);
        
    }

    private float GetInput() {
        if(input == null || input.clip == null || !input.isPlaying)
            return 0f;
        int nChannels = input.clip.channels;
        input.GetSpectrumData(spectrum, CHANNEL, FFTWindow.Hamming);
        return spectrum[0] * 100f;
    }

    private float BeatUpdate(float newValue) {
        prevAudioValue = audioValue;
        audioValue = newValue;

        bool beatTrigger = prevAudioValue > thresholdLevel && audioValue < thresholdLevel;
        beatTrigger |= prevAudioValue <= thresholdLevel && audioValue > thresholdLevel;
        if(beatTrigger && timer > minTime) {
            if(timer > attack + sustain)
                timer = 0f;
            else if(timer > attack)
                timer = attack * (1f - (timer - attack) / sustain);
        }

        timer += Time.deltaTime;
        float f = 0f;
        if(timer <= attack)
            f = timer / attack;
        else if(timer < attack + sustain)
            f = 1f - ((timer - attack) / sustain);
        return f;
    }

    private void TrackInput(float value) {
        if(ampSpectrum == null)
            ampSpectrum = new Queue<float>();
        if(ampSpectrum.Count > SPEC_SAMPLES)
            ampSpectrum.Dequeue();
        ampSpectrum.Enqueue(value);
    }

    public string GetAmpGuide() {
        if(ampSpectrum == null)
            return "- No sound input -";

        int n = ampSpectrum.Count;

        List<float> curSpectrum = new List<float>(n);
        foreach(float v in ampSpectrum)
            curSpectrum.Add(v);
        curSpectrum.Sort();
        string toReturn = "";
        toReturn += GetSpectrumValue(curSpectrum, .90f) + " : ";
        toReturn += GetSpectrumValue(curSpectrum, .95f) + " : ";
        toReturn += GetSpectrumValue(curSpectrum, .99f);
        return toReturn;
    }

    private string GetSpectrumValue(List<float> spectrum, float r) {
        float sample = r * (spectrum.Count - 1);
        float value = spectrum[Mathf.FloorToInt(sample)];
        float bias = value % 1f;
        if(bias > 0f) {
            float biasValue = spectrum[Mathf.CeilToInt(sample)];
            value = ((1f - bias) * value) + (bias * biasValue);
        }
        string toReturn = Mathf.RoundToInt(Mathf.Abs(value)).ToString();
        toReturn.PadLeft(2, '0');
        if(value < 0)
            toReturn = "  -" + toReturn;
        else
            toReturn = "   " + toReturn;

        return toReturn;
    }

    private float UpdateValue(float newValue) {
        float toReturn = newValue;
        value = newValue;

        switch(outputType) {
        case OuputType.Direct:
        case OuputType.Integrate:
            return toReturn;
        case OuputType.Derivative:
            return (newValue - value) / Time.deltaTime;
        default:
            Debug.LogError("Unknown output type: " + outputType);
            return toReturn;
        }
    }

    private object AddTargetValue(object left, object right) {
        if(target == null)
            return null;

        float s = Time.deltaTime;

        switch(target.parameter.type) {
        case FERM_Parameter.Type.Integer:
        case FERM_Parameter.Type.Floating:
            return (float)left + s * (float)right;
        case FERM_Parameter.Type.Vec2:
        case FERM_Parameter.Type.Vector:
            return (Vector3)left + s * (Vector3)right;
        case FERM_Parameter.Type.Quaternion:
            Quaternion toAdd = Quaternion.SlerpUnclamped(Quaternion.identity, (Quaternion)right, s);
            return toAdd * (Quaternion)left;
        default:
            Debug.LogError("Unkown target type: " + target.parameter.type);
            return null;
        }
    }

    private object LerpTargetValue(float t) {
        if(target == null)
            return null;

        switch(target.parameter.type) {
        case FERM_Parameter.Type.Integer:
        case FERM_Parameter.Type.Floating:
            return Mathf.LerpUnclamped(rest_float, beat_float, t);
        case FERM_Parameter.Type.Vec2:
        case FERM_Parameter.Type.Vector:
            return Vector3.LerpUnclamped(rest_vec, beat_vec, t);
        case FERM_Parameter.Type.Quaternion:
            return Quaternion.SlerpUnclamped(rest_quat, beat_quat, t);
        default:
            Debug.LogError("Unkown target type: " + target.parameter.type);
            return null;
        }
    }
}
