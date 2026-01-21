using System;
using UnityEngine;

/// <summary>
/// Modifies the output distance function of a characterizing shape that
/// shares this gameObject.
/// </summary>
[RequireComponent(typeof(FERM_CharacterizingComponent))]
public class FERM_Modifier : FERM_Component {

    public Type type;

    public enum Type {
        Bend, Elongate, Inflate, Mirror,
        ModuloAll, ModuloX, ModuloXZ,
        RepeatAll, RepeatX, RepeatXZ,
        Revolve, Rotate, ScaleGeneral, ScaleUniform,
        Shear, Transform, Translate, Twist, Wobble  
    }

    private class ModifierType {
        public static readonly ModifierType Transform = new ModifierType(
            "InverseTransform(@par0, @par1, @par2, @pos)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector3 shift = (Vector3)parameters[0].GetValue();
                Quaternion rotation = (Quaternion)parameters[1].GetValue();
                float scale = (float)parameters[2].GetValue();
                return PreTransform(pos, shift, rotation, scale);
            },
            "(@fnc * @par2)",
            (float value, FERM_Parameter[] parameters) => {
                float scale = (float)parameters[2].GetValue();
                return value * scale;
            },
            Geometry.Exact,
            new FERM_Parameter("position", Vector3.zero),
            new FERM_Parameter("rotation", Quaternion.identity),
            new FERM_Parameter("scale", 1f)
        );

        public static readonly ModifierType Inflate = new ModifierType(
            "@pos",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                return pos;
            },
            "@fnc - @par0",
            (float value, FERM_Parameter[] parameters) => {
                float amount = (float)parameters[0].GetValue();
                return value - amount;
            },
            Geometry.Exact,
            new FERM_Parameter("amount", .5f)
        );

        public static readonly ModifierType Translate = new ModifierType(
            "(@pos - @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector3 shift = (Vector3)parameters[0].GetValue();
                return pos - shift;
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.Exact,
            new FERM_Parameter("position", Vector3.zero)
        );

        public static readonly ModifierType Rotate = new ModifierType(
            "InverseRotate(@par0, @pos)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Quaternion rot = (Quaternion)parameters[0].GetValue();
                return Quaternion.Inverse(rot) * pos;
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.Exact,
            new FERM_Parameter("rotation", Quaternion.identity)
        );

        public static readonly ModifierType Mirror = new ModifierType(
            "Mirror(@pos, @par0, @par1)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector3 normal = (Vector3)parameters[0].GetValue();
                float d = (float)parameters[1].GetValue();
                return pos.Mirror(normal, d);
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.Exact,
            new FERM_Parameter("Normal", Vector3.right),
            new FERM_Parameter("d", 0f)
        );

        public static readonly ModifierType ScaleUniform = new ModifierType(
            "(@pos / @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float scale = (float)parameters[0].GetValue();
                return pos / scale;
            },
            "(@fnc * @par0)",
            (float value, FERM_Parameter[] parameters) => {
                float scale = (float)parameters[0].GetValue();
                return value * scale;
            },
            Geometry.Exact,
            new FERM_Parameter("scale", 1f)
        );

        public static readonly ModifierType ScaleGeneral = new ModifierType(
            "InverseScale(@par0, @pos)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector3 scale = (Vector3)parameters[0].GetValue();
                return pos.InverseScale(scale);
            },
            "Scale(@par0, @fnc)",
            (float value, FERM_Parameter[] parameters) => {
                Vector3 scale = (Vector3)parameters[0].GetValue();
                return value * scale.Min();
            },
            Geometry.Bounded,
            new FERM_Parameter("scale", Vector3.one)
        );

        public static readonly ModifierType Wobble = new ModifierType(
            "@pos",
            (Vector3 pos, FERM_Parameter[] parameters) => {return pos;},
            "(@fnc + Wobble(@par0, @par1, @pos))",
            (float value, FERM_Parameter[] parameters) => {
                Vector3 freq = (Vector3)parameters[0].GetValue();
                float amp = (float)parameters[1].GetValue();
                Vector3 vec = FERM_Caster.inputPos;
                return value + Mathf.Cos(freq.x * vec.x) * Mathf.Cos(freq.y * vec.y) * Mathf.Cos(freq.z * vec.z) * amp;
            },
            Geometry.Distorted,
            new FERM_Parameter("frequency", 10f * Vector3.one),
            new FERM_Parameter("amplitude", .05f)
        );

        public static readonly ModifierType RepeatAll = new ModifierType(
            "Repeat(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector3 c = (Vector3)parameters[0].GetValue();
                return pos.Rep(c);
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.ExactConditonal,
            new FERM_Parameter("CellSize", 2*Vector3.one)
        );

        public static readonly ModifierType RepeatX = new ModifierType(
            "RepeatX(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float c = (float)parameters[0].GetValue();
                return new Vector3(pos.x.Rep(c), pos.y, pos.z);
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.ExactConditonal,
            new FERM_Parameter("CellSize", 1f)
        );

        public static readonly ModifierType RepeatXZ = new ModifierType(
            "RepeatXZ(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector2 c = (Vector2)parameters[0].GetValue();
                return new Vector3(pos.x.Rep(c.x), pos.y, pos.z.Rep(c.y));
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.ExactConditonal,
            new FERM_Parameter("CellSize", 2*Vector2.one)
        );

        public static readonly ModifierType ModuloAll = new ModifierType(
            "Modulo(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector3 c = (Vector3)parameters[0].GetValue();
                return pos.Mod(c);
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.ExactConditonal,
            new FERM_Parameter("CellSize", 2*Vector3.one)
        );

        public static readonly ModifierType ModuloX = new ModifierType(
            "ModuloX(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float c = (float)parameters[0].GetValue();
                return new Vector3(pos.x.Mod(c), pos.y, pos.z);
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.ExactConditonal,
            new FERM_Parameter("CellSize", 1f)
        );

        public static readonly ModifierType ModuloXZ = new ModifierType(
            "ModuloXZ(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector2 c = (Vector2)parameters[0].GetValue();
                return new Vector3(pos.x.Mod(c.x), pos.y, pos.z.Mod(c.y));
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.ExactConditonal,
            new FERM_Parameter("CellSize", 2*Vector2.one)
        );

        public static readonly ModifierType Elongate = new ModifierType(
            "Elongate(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector3 c = (Vector3)parameters[0].GetValue();
                return pos.Elongate(c);
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.Exact,
            new FERM_Parameter("Amount", Vector3.up)
        );

        public static readonly ModifierType Twist = new ModifierType(
            "Twist(@pos, @par0, @par1)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                Vector3 axis = ((Vector3)parameters[0].GetValue()).normalized;
                float power = (float)parameters[1].GetValue();
                float angle = Vector3.Dot(p, axis) * power * 36f;
                return Quaternion.AngleAxis(angle, axis) * p;
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.Distorted,
            new FERM_Parameter("Axis", Vector3.up),
            new FERM_Parameter("Power", 1f)
        );

        public static readonly ModifierType Bend = new ModifierType(
            "Bend(@pos, @par0, @par1, @par2)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                Vector3 axis = ((Vector3)parameters[0].GetValue()).normalized;
                Vector3 direction = ((Vector3)parameters[1].GetValue()).normalized;

                float power = (float)parameters[2].GetValue();
                float angle = Vector3.Dot(p, direction) * power * 36f;
                axis = (axis - Vector3.Dot(direction, axis) * direction).normalized;
                return Quaternion.AngleAxis(angle, axis) * p;
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.Distorted,
            new FERM_Parameter("Axis", Vector3.forward),
            new FERM_Parameter("Direction", Vector3.right),
            new FERM_Parameter("Power", 1f)
        );

        public static readonly ModifierType Shear = new ModifierType(
            "Shear(@pos, @par0, @par1, @par2, @par3, @par4, @par5)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float xy = (float)parameters[0].GetValue();
                float xz = (float)parameters[1].GetValue();
                float yx = (float)parameters[2].GetValue();
                float yz = (float)parameters[3].GetValue();
                float zx = (float)parameters[4].GetValue();
                float zy = (float)parameters[5].GetValue();

                Matrix4x4 m = new Matrix4x4(
                    new Vector4(1f, yx, zx),
                    new Vector4(xy, 1f, zy),
                    new Vector4(xz, yz, 1f),
                    new Vector4(0f, 0f, 0f, 1f));
                return m.MultiplyPoint(pos);
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.Distorted,
            new FERM_Parameter("xy", 1f), new FERM_Parameter("xz", 0f), new FERM_Parameter("yx", 0f),
            new FERM_Parameter("yz", 0f), new FERM_Parameter("zx", 0f), new FERM_Parameter("zy", 0f)
        );

        public static readonly ModifierType Revolve = new ModifierType(
            "Revolve(@pos, @par0)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                Quaternion orientation = (Quaternion)parameters[0].GetValue();

                Vector3 axis = orientation * Vector3.forward;
                Vector3 sampleDir = orientation * Vector3.right;

                Vector3 ax = Vector3.Dot(p, axis) * axis;

                float l = (p - ax).magnitude;
                return l * sampleDir + ax;
            },
            "@fnc",
            (float value, FERM_Parameter[] parameters) => { return value; },
            Geometry.Exact,
            new FERM_Parameter("Orientation", Quaternion.Euler(90f, 0f, 0f))
        ); 

        public readonly string inputFunction;
        public readonly Func<Vector3, FERM_Parameter[], Vector3> Pre;
        public readonly string outputFunction;
        public readonly Func<float, FERM_Parameter[], float> Post;
        public readonly FERM_Parameter[] parameters;
        public readonly Geometry geometry;

        private ModifierType(string inputFunction, Func<Vector3, FERM_Parameter[], Vector3> Pre, string outputFunction, Func<float, FERM_Parameter[], float> Post, Geometry geometry, params FERM_Parameter[] parameters) {
            this.inputFunction = inputFunction;
            this.Pre = Pre;
            this.outputFunction = outputFunction;
            this.Post = Post;
            this.geometry = geometry;
            this.parameters = parameters;
        }

        public string ApplyInput(string function) {
            return FERM_Util.Insert(function, "pos", inputFunction);
        }

        public string ApplyOutput(string function) {
            return FERM_Util.Insert(outputFunction, "fnc", function);
        }

        public string ApplyBoth(string function) {
            return ApplyOutput(ApplyInput(function));
        }
    }

    private static ModifierType GetModifier(Type type) {
        System.Type shapeType = typeof(ModifierType);
        return (ModifierType)shapeType.GetField(type.ToString()).GetValue(null);
    }


    public string ApplyInput(string function) {
        string toReturn = GetModifier(type).ApplyInput(function);
        return FillInParameters(toReturn, true);
    }

    public string ApplyOutput(string function) {
        string toReturn = GetModifier(type).ApplyOutput(function);
        return FillInParameters(toReturn, true);
    }

    /// <summary>
    /// Outputs a distance function that is the given function 
    /// with this modifier applied to it. The input value should 
    /// still contain "@pos" for this method to work. For safety 
    /// sake, you may not want to leave parameter flags ("@par0") 
    /// in the input function.
    /// </summary>
    public string Apply(string function) {
        string toReturn = GetModifier(type).ApplyBoth(function);
        return FillInParameters(toReturn, false);
    }

    public static string ApplyTransform(string inputFunction, string pos, string rot, string scl) {
        string tp = "InverseTransform(" + pos + ", " + rot + ", " + scl + ", @pos)";
        string toReturn = FERM_Util.Insert(inputFunction, "pos", tp);
        return "(" + toReturn + " * " + scl + ")";
    }

    public static Vector3 PreTransform(Vector3 pos, Vector3 shift, Quaternion rot, float scale) {
        return Quaternion.Inverse(rot) * (pos - shift) / scale;
    }

    public static float PostTransform(float value, Vector3 shift, Quaternion rot, float scale) {
        return value * scale;
    }

    protected override FERM_Parameter[] GetParameters() {
        return GetModifier(type).parameters;
    }

    public override int GetHashCode() {
        return 199 + (int)type;
    }

    public override Geometry GetGeometry() {
        return GetModifier(type).geometry;
    }

    public Vector3 Pre(Vector3 pos) {
        return GetModifier(type).Pre(pos, parameters);
    }
    
    public float Post(float value) {
        return GetModifier(type).Post(value, parameters);
    }
}
