using UnityEngine;
using System.Text.RegularExpressions;

public static class FERM_Util {

    private const float EPSILON = 1e-4f;

    /// <summary>
    /// Returns a Vector4 containing x,y,z,w values of
    /// the given quaternion
    /// </summary>
    public static Vector4 ToVector(Quaternion q) {
        return new Vector4(q.x, q.y, q.z, q.w).normalized;
    }

    /// <summary>
    /// Returns a string representing the given index 
    /// with a consistent amount of digits.
    /// e.g. NumString(45,999) = "045"
    /// </summary>
    public static string NumString(int index, int max) {
        int length = Mathf.CeilToInt(Mathf.Log10(max));
        return index.ToString().PadLeft(length, '0');
    }

    /// <summary>
    /// Returns given template string where "@match" is replaced by 
    /// the given content, in a consistent format.
    /// </summary>
    public static string Insert(string template, string match, string content) {
        //user regex to determine indentation of our target '@'
        string regex = @"([ \t]*)\@(" + match + @")(\W|$)";
        Match m = Regex.Match(template, regex);
        if(!m.Success) {
            Debug.LogError("Could not find @" + match + " in template: " + template);
            return template;
        }

        //insert indentation spaces
        string space = m.Groups[1].Value;
        content.Replace("\r\n", "\r\n" + space);
        return template.Replace('@' + match, content);
    }

    /// <summary>
    /// Replace @match with a dedicated slot form that can be filled 
    /// in and changed as needed trough InsertSlot(). 
    /// The slot has the form "//#match\r\n ... //#"
    /// </summary>
    public static string MakeSlot(string template, string match) {
        string regex = "@" + match;
        string slot = "//#" + match + "\r\n//#";
        return Regex.Replace(template, regex, slot);
    }

    /// <summary>
    /// True if the content has a slot of the given 
    /// match name, as made by MakeSlot()
    /// </summary>
    public static bool HasSlot(string template, string match) {
        string regex = @"(?s)\/{2}#" + match + @".*?\/{2}#";
        return Regex.Match(template, regex).Length >= 0;
    }

    /// <summary>
    /// Change the content held in the insert Slot with the given 
    /// match name, as made by MakeSlot()
    /// </summary>
    public static string InsertSlot(string template, string match, string content) {
        string regex = @"(?s)\/{2}#" + match + @".*?\/{2}#";
        string slot = "//#" + match + "\r\n" + content + "\r\n//#";
        return Regex.Replace(template, regex, slot);
    }

    /// <summary>
    /// Returns a uniform scaling vector based on the given value.
    /// This method is specifically designed to be sensetive to 
    /// single changes, so that a user can easily rescale via the editor.
    /// </summary>
    public static Vector3 EqualizeScale(Vector3 scale) {
        float factor = Vector3.Dot(scale, Vector3.one) / 3f;
        if(Mathf.Abs(scale.x - scale.y) < EPSILON)
            factor = scale.z;
        else if(Mathf.Abs(scale.z - scale.y) < EPSILON)
            factor = scale.x;
        else if(Mathf.Abs(scale.x - scale.z) < EPSILON)
            factor = scale.y;
        return factor * Vector3.one;
    }

    public static bool IsUnityTransformPar(FERM_Parameter par) {
        return par.name.StartsWith("t_");
    }
    
    /// <summary>
    /// Return true if the local transform variables are all
    /// their default values
    /// </summary>
    public static bool IsIdentity(Transform t) {
        if(t.localPosition != Vector3.zero)
            return false;
        if(t.localRotation != Quaternion.identity)
            return false;
        return t.localScale == Vector3.one;
    }

    /// <summary>
    /// Returns lerp factor needed to perform an exponential decay time step, 
    /// using the given half life time and Time.deltaTime.
    /// Use as follows: value = Lerp(value, goalValue, factor);
    /// </summary>
    public static float LerpExpFactor(float halfLife) {
        return LerpExpFactor(halfLife, Time.deltaTime);
    }

    /// <summary>
    /// Returns lerp factor needed to perform an exponential decay time step, 
    /// using the given half life time and time step.
    /// Use as follows: value = Lerp(value, goalValue, factor);
    /// </summary>
    public static float LerpExpFactor(float halfLife, float timeStep) {
        if(halfLife <= 0f)
            return 1f;
        else if(float.IsNaN(halfLife) || float.IsPositiveInfinity(halfLife))
            return 0f;
        float a = timeStep * Mathf.Log(2f) / (2 * halfLife);
        return 2 * a / (1 + a);
    }

    public static float FactorToDb(float input) {
        return Mathf.Log(10f, input / 20f) * 10f;
    }

    public static float DbToFactor(float input) {
        return Mathf.Pow(10f, input / 10f) * 20f;
    }



    /* ---------------------------------------------------------------------------------
     * --------------------------- DISTANCE ESTIMATOR HELPERS --------------------------
     * ---------------------------------------------------------------------------------
     */

    public static Vector2 Flat(Vector3 pos) {
        return new Vector2(pos.x, pos.z);
    }

    public static float FlatDistance(Vector3 pos, float flatDistance) {
        return new Vector2(Mathf.Max(flatDistance, 0f), pos.y).magnitude;
    }

    public static float Dot2(Vector3 a) {
        return Vector3.Dot(a, a);
    }

    public static Vector3 Abs(Vector3 v) {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    public static Vector2 Abs(Vector2 v) {
        return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
    }

    public static Vector3 Shift(this Vector3 v, float a) {
        return new Vector3(v.x + a, v.y + a, v.z + a);
    }

    public static Vector2 Shift(this Vector2 v, float a) {
        return new Vector2(v.x + a, v.y + a);
    }

    public static Vector3 Max(this Vector3 v, float max) {
        return new Vector3(Mathf.Max(v.x, max), Mathf.Max(v.y, max), Mathf.Max(v.z, max));
    }

    public static Vector3 Min(this Vector3 v, float min) {
        return new Vector3(Mathf.Min(v.x, min), Mathf.Min(v.y, min), Mathf.Min(v.z, min));
    }

    public static float Max(this Vector3 v) {
        return Mathf.Max(v.x, v.y, v.z);
    }

    public static float Min(this Vector3 v) {
        return Mathf.Min(v.x, v.y, v.z);
    }

    public static Vector2 Max(this Vector2 v, float max) {
        return new Vector2(Mathf.Max(v.x, max), Mathf.Max(v.y, max));
    }

    public static Vector2 Min(this Vector2 v, float min) {
        return new Vector2(Mathf.Min(v.x, min), Mathf.Min(v.y, min));
    }

    public static Vector2 XZ(this Vector3 v) {
        return new Vector2(v.x, v.z);
    }

    public static Vector2 XY(this Vector3 v) {
        return new Vector2(v.x, v.y);
    }

    public static Vector2 YX(this Vector3 v) {
        return new Vector2(v.y, v.x);
    }

    public static Vector2 YZ(this Vector3 v) {
        return new Vector2(v.y, v.z);
    }

    public static Vector2 ZY(this Vector3 v) {
        return new Vector2(v.z, v.y);
    }

    public static Vector2 ZX(this Vector3 v) {
        return new Vector2(v.z, v.x);
    }

    public static Vector3 XZ(this Vector3 v, Vector2 a) {
        return new Vector3(a.x, v.y, a.y);
    }

    public static Vector3 XY(this Vector3 v, Vector2 a) {
        return new Vector3(a.x, a.y, v.z);
    }

    public static Vector3 YX(this Vector3 v, Vector2 a) {
        return new Vector3(a.y, a.x, v.z);
    }

    public static Vector3 YZ(this Vector3 v, Vector2 a) {
        return new Vector3(v.x, a.x, a.y);
    }

    public static Vector3 ZY(this Vector3 v, Vector2 a) {
        return new Vector3(v.x, a.y, a.x);
    }

    public static Vector3 ZX(this Vector3 v, Vector2 a) {
        return new Vector3(a.y, v.y, a.x);
    }

    public static Vector3 XZ(this Vector2 v) {
        return new Vector3(v.x, 0f, v.y);
    }

    public static Vector3 YZX(this Vector3 v) {
        return new Vector3(v.y, v.z, v.x);
    }

    public static Vector3 ZXY(this Vector3 v) {
        return new Vector3(v.z, v.x, v.y);
    }

    public static Vector3 Mirror(this Vector3 v, Vector3 normal) {
        float x = 2f * Vector3.Dot(v, normal) / normal.sqrMagnitude;
        return v - Mathf.Max(x, 0f) * normal;
    }

    public static Vector3 Mirror(this Vector3 v, Vector3 normal, float d) {
        normal = normal.normalized;
        float x = 2f * Vector3.Dot(v - normal * d, normal);
        return v - Mathf.Max(x, 0f) * normal;
    }

    public static Vector3 InverseScale(this Vector3 v, Vector3 s) {
        return new Vector3(v.x / s.x, v.y / s.y, v.z / s.z);
    }

    public static Vector3 Elongate(this Vector3 v, Vector3 h) {
        return v - v.Clamp(-h / 2f, h / 2f);
    }

    public static Vector3 Clamp(this Vector3 v, float min, float max) {
        return new Vector3(Mathf.Clamp(v.x, min, max), Mathf.Clamp(v.y, min, max), Mathf.Clamp(v.z, min, max));
    }

    public static Vector3 Clamp(this Vector3 v, Vector3 min, Vector3 max) {
        return new Vector3(Mathf.Clamp(v.x, min.x, max.x), Mathf.Clamp(v.y, min.y, max.y), Mathf.Clamp(v.z, min.z, max.z));
    }

    public static float Rep(this float v, float r) {
        return Mathf.Repeat(v, r);
    }

    public static Vector2 Rep(Vector2 v, Vector2 r) {
        return new Vector2(v.x.Rep(r.x), v.y.Rep(r.y));
    }

    public static Vector3 Rep(this Vector3 v, Vector3 r) {
        return new Vector3(v.x.Rep(r.x), v.y.Rep(r.y), v.z.Rep(r.z));
    }

    public static float Mod(this float v, float r) {
        return Mathf.Abs(v % r);
    }

    public static Vector2 Mod(Vector2 v, Vector2 r) {
        return new Vector2(v.x.Mod(r.x), v.y.Mod(r.y));
    }

    public static Vector3 Mod(this Vector3 v, Vector3 r) {
        return new Vector3(v.x.Mod(r.x), v.y.Mod(r.y), v.z.Mod(r.z));
    }

    public static Vector2 CMul(this Vector2 a, Vector2 b) {
        return new Vector2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
    }


    /* ---------------------------------------------------------------------------------
     * ------------------------ ELEMENTARY DISTANCE ESTIMATORS -------------------------
     * ---------------------------------------------------------------------------------
     */

    public static float DF_Sphere(Vector3 pos, float radius) {
        return pos.magnitude - radius;
    }

    public static float DF_Box(Vector3 pos, Vector3 size) {
        Vector3 d = Abs(pos) - size / 2f;
        return Max(d, 0f).magnitude + Mathf.Min(Mathf.Max(d.x, Mathf.Max(d.y, d.z)), 0f);
    }

    public static float DF_InfinitePipe(Vector3 pos, float r1, float r2) {
        float r = pos.XZ().magnitude;
        return (Mathf.Abs(2f * r - r1 - r2) + r1 - r2) / 2f;
    }

    public static float DF_InfinitePyramid(Vector3 pos, Vector2 slope) {
        Vector3 xn = new Vector3(slope.x, 1f, 0f).normalized;
        Vector3 yn = new Vector3(0f, 1f, slope.y).normalized;
        Vector3 p = new Vector3(Mathf.Abs(pos.x), pos.y, Mathf.Abs(pos.z));
        return Mathf.Max(Vector3.Dot(xn, p), Vector3.Dot(yn, p));
    }

    public static float DF_Triangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c) {
        Vector3 ba = b - a; Vector3 pa = p - a;
        Vector3 cb = c - b; Vector3 pb = p - b;
        Vector3 ac = a - c; Vector3 pc = p - c;
        Vector3 nor = Vector3.Cross(ba, ac);

        return Mathf.Sqrt(
        (Mathf.Sign(Vector3.Dot(Vector3.Cross(ba,nor),pa)) +
            Mathf.Sign(Vector3.Dot(Vector3.Cross(cb,nor),pb)) +
            Mathf.Sign(Vector3.Dot(Vector3.Cross(ac,nor),pc))<2f)
            ?
            Mathf.Min(
            (ba * Mathf.Clamp01(Vector3.Dot(ba,pa)/ ba.sqrMagnitude)-pa).sqrMagnitude,
            (cb * Mathf.Clamp01(Vector3.Dot(cb,pb)/ cb.sqrMagnitude)-pb).sqrMagnitude,
            (ac * Mathf.Clamp01(Vector3.Dot(ac,pc)/ ac.sqrMagnitude)-pc).sqrMagnitude)
            :
            Vector3.Dot(nor,pa)*Vector3.Dot(nor,pa)/nor.sqrMagnitude);
    }

    public static float DF_Line(Vector2 p, float length) {
        Vector2 l = new Vector2(length, 0f);
        return DF_Quad(p - l / 2f, l, 0f);
    }

    public static float DF_Quad(Vector2 p, Vector2 size, float round) {
        Vector2 delta = Shift(Abs(p) - size / 2f, round);
        return Max(delta, 0f).magnitude - round;
    }

    public static float InfiniteCone(Vector3 p, float slope) {
        Vector2 c = new Vector2(slope, 1f).normalized;
        float q = p.XZ().magnitude;
        return Vector2.Dot(c, new Vector2(q, p.y));
    }

    public static float DF_Tetrahedron(Vector3 p, float size) {
        p /= size;

        p = p.Mirror(new Vector3(.37f, 1f, .622f));
        p = p.Mirror(new Vector3(.37f, 1f, -.622f));
        p = p.Mirror(new Vector3(-1f, 1.414f, 0f));

        Vector3 v1 = new Vector3(.9428f, -.3333f, 0f);
        Vector3 v2 = new Vector3(-.4714f, -.3333f, .8165f);
        Vector3 v3 = new Vector3(-.4714f, -.3333f, -.8165f);

        return DF_Triangle(p, v1, v2, v3) * size;
    }
}
