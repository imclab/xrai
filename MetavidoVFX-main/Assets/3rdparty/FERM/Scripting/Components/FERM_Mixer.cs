using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mixes together characterizing shapes of child objects
/// in a pair-wise fashion. Larger numbers of objects are 
/// paired together recursively.
/// </summary>
public class FERM_Mixer : FERM_CharacterizingComponent {

    //connection cache
    private List<FERM_CharacterizingComponent> children;

    private const string NULL_FUNC = "1e+6"; //used to indicate a blank distance function

    public Type type;

    public enum Type {
        Union, UnionSmooth, UnionWobble, Intersect,
        IntersectSmooth, Difference, DifferenceSmooth, 

    }

    private class MixerType {
        public static readonly MixerType
            Union = new MixerType("Union(@in1, @in2)",
            (float a, float b, FERM_Parameter[] parameters) => {
                return Mathf.Min(a, b);
            },
            Geometry.Exact
        );

        public static readonly MixerType
            Intersect = new MixerType("Intersect(@in1, @in2)",
            (float a, float b, FERM_Parameter[] parameters) => {
                return Mathf.Max(a, b);
            },
            Geometry.Bounded
        );

        public static readonly MixerType
            Difference = new MixerType("Difference(@in1, @in2)",
            (float a, float b, FERM_Parameter[] parameters) => {
                return Mathf.Max(-a, b);
            },
            Geometry.Bounded
        );

        public static readonly MixerType
            UnionSmooth = new MixerType("SmoothUnion(@in1, @in2, @par0)",
            (float a, float b, FERM_Parameter[] parameters) => {
                float k = (float)parameters[0].GetValue();
                float h = Mathf.Clamp01(.5f + .5f * (b - a) / k);
                return Mathf.Lerp(b, a, h) - k * h * (1f - h);
            },
            Geometry.Bounded,
            new FERM_Parameter("strength", .5f) 
        );

        public static readonly MixerType
            IntersectSmooth = new MixerType("SmoothIntersect(@in1, @in2, @par0)",
            (float a, float b, FERM_Parameter[] parameters) => {
                float k = (float)parameters[0].GetValue();
                float h = Mathf.Clamp01(.5f - .5f * (b - a) / k);
                return Mathf.Lerp(b, a, h) + k * h * (1f - h);
            },
            Geometry.Bounded,
            new FERM_Parameter("strength", .5f)
        );

        public static readonly MixerType
            DifferenceSmooth = new MixerType("SmoothDifference(@in1, @in2, @par0)",
            (float a, float b, FERM_Parameter[] parameters) => {
                float k = (float)parameters[0].GetValue();
                float h = Mathf.Clamp01(.5f - .5f * (b + a) / k);
                return Mathf.Lerp(b, -a, h) + k * h * (1f - h);
            },
            Geometry.Bounded,
            new FERM_Parameter("strength", .5f)
        );

        public static readonly MixerType
            UnionWobble = new MixerType("DeformedMix(@in1, @in2, @par0, @par1, @par2, @par3)",
            (float a, float b, FERM_Parameter[] parameters) => {
                float k = (float)parameters[0].GetValue();
                float f = (float)parameters[1].GetValue();
                float A = (float)parameters[2].GetValue();
                float p = (float)parameters[3].GetValue();

                float h = Mathf.Clamp01(.5f + .5f * (b - a) / k);
                h = h + A * (h - h * h) * Mathf.Sin((f * h * k + p) * Mathf.PI);
                return Mathf.Lerp(b, a, h) - k * h * (1f - h);
            },
            Geometry.BoundedConditonal,
            new FERM_Parameter("smooth", .5f),
            new FERM_Parameter("frequency", 10f),
            new FERM_Parameter("amplitude", 1f),
            new FERM_Parameter("phase shift", 0f)
        );

        public readonly string mixFunction;
        public Func<float, float, FERM_Parameter[], float> Mix;
        public readonly Geometry geometry;
        public readonly FERM_Parameter[] parameters;

        private MixerType(string mixFunction, Func<float, float, FERM_Parameter[], float> Mix, Geometry geometry, params FERM_Parameter[] parameters) {
            this.mixFunction = mixFunction;
            this.Mix = Mix;
            this.geometry = geometry;
            this.parameters = parameters;
        }
    }

    private MixerType GetMixer(Type type) {
        System.Type shapeType = typeof(MixerType);
        return (MixerType)shapeType.GetField(type.ToString()).GetValue(null);
    }

    public override string GetParametrizedDF() {
        List<String> distanceFunctions = GetChildren();
        string toReturn = Mix(GetMixer(type).mixFunction, distanceFunctions);
        return FillInParameters(toReturn, true);
    }

    public static string Union(List<String> distanceFunctions) {
        return Mix(MixerType.Union.mixFunction, distanceFunctions);
    }

    private static string Mix(string mixFunction, List<String> distanceFunctions) {
        if(distanceFunctions.Count == 0)
            return NULL_FUNC;

        while(distanceFunctions.Count > 1) {
            int i = 0;
            while(i + 1 < distanceFunctions.Count) {
                distanceFunctions[i] = Mix(mixFunction, distanceFunctions[i], distanceFunctions[i + 1]);
                distanceFunctions.RemoveAt(++i);
            }
        }

        return distanceFunctions[0];
    }

    private static string Mix(string mixFunction, string in1, string in2) {
        mixFunction = FERM_Util.Insert(mixFunction, "in1", in1);
        mixFunction = FERM_Util.Insert(mixFunction, "in2", in2);
        return mixFunction;
    }

    private List<string> GetChildren() {
        List<string> toReturn = new List<string>();
        children = new List<FERM_CharacterizingComponent>();
        int totalChildren = transform.childCount;
        for(int i = 0; i < totalChildren; i++) {
            FERM_CharacterizingComponent shape = 
                transform.GetChild(i).GetComponent<FERM_CharacterizingComponent>();
            if(shape && shape.active) {
                children.Add(shape);
                toReturn.Add(shape.GetFullDF());
            }
        }
        return toReturn;
    }

    protected override FERM_Parameter[] GetParameters() {
        return GetMixer(type).parameters;
    }

    public override int GetHashCode() {
        return 498 + (int)type;
    }

    public override Geometry GetGeometry() {
        return GetMixer(type).geometry;
    }

    protected override Vector3 Pre(Vector3 pos) {
        return pos;
    }

    protected override float InnerDF(Vector3 pos) {
        List<float> values = new List<float>(children.Count);
        foreach(FERM_CharacterizingComponent shape in children)
            values.Add(shape.DF(pos));

        if(values.Count == 0)
            return float.PositiveInfinity;

        while(values.Count > 1) {
            int i = 0;
            while(i + 1 < values.Count) {
                values[i] = Mix(values[i], values[i + 1]);
                values.RemoveAt(++i);
            }
        }

        return values[0];
    }

    private float Mix(float a, float b) {
        return GetMixer(type).Mix(a, b, parameters);
    }

    protected override float Post(float value) {
        return value;
    }
}
