using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Primitive shapes are elementary characterizing shapes.
/// They require a position input (+ parameters)
/// and output a distance function corresponding to a
/// primitive shape, such as a sphere or box.
/// </summary>
public class FERM_Primitive : FERM_Shape3D {

    public Type type;

    public enum Type {
        Arch, Box, RoundedBox, Pipe, InfinitePipe, Plane, Point, Pyramid,
        Sphere, Ellipsoid, Torus, Capsule, Cone, CappedCone,
        RoundedCone, Cylinder, InfiniteCylinder, RoundedCylinder,
        TriPrism, HexPrism, Tetrahedron, Octahedron
    }

    private class PrimitiveType {
        public static readonly PrimitiveType
            Sphere = new PrimitiveType("Sphere(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float radius = (float)parameters[0].GetValue();
                return FERM_Util.DF_Sphere(pos, radius);
            },
            Geometry.Exact,
            new FERM_Parameter("radius", 1f)
        );

        public static readonly PrimitiveType
            Box = new PrimitiveType("Box(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector3 size = (Vector3)parameters[0].GetValue();
                return FERM_Util.DF_Box(pos, size);
            },
            Geometry.Exact,
            new FERM_Parameter("size", Vector3.one)
        );

        public static readonly PrimitiveType
            RoundedBox = new PrimitiveType("RoundedBox(@pos, @par0, @par1)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector3 size = (Vector3)parameters[0].GetValue();
                float round = (float)parameters[1].GetValue();
                Vector3 d = FERM_Util.Abs(pos) - size / 2f;
                return d.Shift(round).Max(0f).magnitude - round;
            },
            Geometry.Exact,
            new FERM_Parameter("size", Vector3.one),
            new FERM_Parameter("round", .1f)
        );

        public static readonly PrimitiveType
            Torus = new PrimitiveType("Torus(@pos, float2(@par0, @par1))",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float radius = (float)parameters[0].GetValue();
                float thickness = (float)parameters[1].GetValue();
                Vector2 r = new Vector2(pos.XZ().magnitude - radius, pos.y);
                return r.magnitude - thickness;
            },
            Geometry.Exact,
            new FERM_Parameter("radius", 1f),
            new FERM_Parameter("thickness", .2f)
        );

        public static readonly PrimitiveType
            Plane = new PrimitiveType("Plane(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float thickness = (float)parameters[0].GetValue();
                return Mathf.Abs(pos.y) - thickness;
            },
            Geometry.Exact,
            new FERM_Parameter("thickness", 0f)
        );

        public static readonly PrimitiveType
            Cylinder = new PrimitiveType("Cylinder(@pos, float2(@par0, @par1))",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float r = (float)parameters[0].GetValue();
                float h = (float)parameters[1].GetValue();
                Vector2 d = FERM_Util.Abs(new Vector2(pos.XZ().magnitude, pos.y)) - new Vector2(r, h);
                return Mathf.Min(Mathf.Max(d.x, d.y), 0f) + d.Max(0f).magnitude - 0.01f;
            },
            Geometry.Exact,
            new FERM_Parameter("radius", .5f),
            new FERM_Parameter("height", .5f)
        );

        public static readonly PrimitiveType
            InfiniteCylinder = new PrimitiveType("InfiniteCylinder(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float radius = (float)parameters[0].GetValue();
                return pos.XZ().magnitude - radius;
            },
            Geometry.Exact,
            new FERM_Parameter("radius", .5f)
        );

        public static readonly PrimitiveType
            Capsule = new PrimitiveType("Capsule(@pos, @par0, @par1)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                float h = (float)parameters[0].GetValue();
                float r = (float)parameters[1].GetValue();
                h -= 2 * r;
                p.y -= Mathf.Clamp(p.y, -h / 2f, h / 2f);
                return p.magnitude - r;
            },
            Geometry.Exact,
            new FERM_Parameter("height", 2f),
            new FERM_Parameter("radius", .5f)
        );

        public static readonly PrimitiveType
            Pyramid = new PrimitiveType("Pyramid(@pos, @par0, @par1)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector2 slope = (Vector2)parameters[0].GetValue();
                float h = (float)parameters[1].GetValue();
                return Mathf.Max(FERM_Util.DF_InfinitePyramid(pos, slope), -h - pos.y);
            },
            Geometry.BoundedConditonal, 
            new FERM_Parameter("slope", Vector2.one),
            new FERM_Parameter("height", 1f)
        );

        public static readonly PrimitiveType
            InfinitePipe = new PrimitiveType("InfinitePipe(@pos, @par0, @par1)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float r1 = (float)parameters[0].GetValue();
                float r2 = (float)parameters[1].GetValue();
                return FERM_Util.DF_InfinitePipe(pos, r1, r2);
            },
            Geometry.Exact,
            new FERM_Parameter("innerRadius", 1f),
            new FERM_Parameter("outerRadius", 1f)
        );

        public static readonly PrimitiveType
            Pipe = new PrimitiveType("Pipe(@pos, @par0, @par1, @par2)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float r1 = (float)parameters[0].GetValue();
                float r2 = (float)parameters[1].GetValue();
                float len = (float)parameters[2].GetValue();

                float r = pos.XZ().magnitude;
                float y = Mathf.Abs(pos.y) - len / 2f;

                if(y < 0)
                    return (FERM_Util.DF_InfinitePipe(pos, r1, r2));
                if(r > r2)
                    return new Vector2(r - r2, y).magnitude;
                if(r < r1)
                    return new Vector2(r1 - r, y).magnitude;
                return y;
            },
            Geometry.Exact,
            new FERM_Parameter("innerRadius", .4f),
            new FERM_Parameter("outerRadius", .5f),
            new FERM_Parameter("length", 1f)
        );

        public static readonly PrimitiveType
            Octahedron = new PrimitiveType("Octahedron(@pos, @par0)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                float s = (float)parameters[0].GetValue();

                p = FERM_Util.Abs(p);
                float m = p.x + p.y + p.z - s;
                Vector3 q;
                if(3f * p.x < m)
                    q = p;
                else if(3f * p.y < m)
                    q = p.YZX();
                else if(3f * p.z < m)
                    q = p.ZXY();
                else
                    return m * 0.57735027f;

                float k = Mathf.Clamp(0.5f * (q.z - q.y + s), 0f, s);
                return new Vector3(q.x, q.y - s + k, q.z - k).magnitude;
            },
            Geometry.Exact,
            new FERM_Parameter("size", 1f)
        );

         public static readonly PrimitiveType
            HexPrism = new PrimitiveType("HexPrism(@pos, @par0, @par1)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                float size = (float)parameters[0].GetValue();
                float height = (float)parameters[1].GetValue();

                Vector3 p = FERM_Util.Abs(pos);
                return Mathf.Max(
                    p.y - height / 2f,
                    Mathf.Max(p.z * .866025f + p.x * .5f, p.x) - size
                );
            },
            Geometry.Exact,
            new FERM_Parameter("size", .5f),
            new FERM_Parameter("height", 1f)
        );

         public static readonly PrimitiveType
            TriPrism = new PrimitiveType("TriPrism(@pos, @par0, @par1)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                float size = (float)parameters[0].GetValue();
                float height = (float)parameters[1].GetValue();

                Vector3 q = FERM_Util.Abs(p);
                return Mathf.Max(
                    q.y - height / 2f,
                    Mathf.Max(q.x * .866025f + p.z * .5f, -p.z) - size * .5f);
            },
            Geometry.Exact,
            new FERM_Parameter("size", .5f),
            new FERM_Parameter("height", 1f)
        );

         public static readonly PrimitiveType
            Tetrahedron = new PrimitiveType("Tetrahedron(@pos, @par0)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                float size = (float)parameters[0].GetValue();
                return FERM_Util.DF_Tetrahedron(p, size);
            },
            Geometry.Exact,
            new FERM_Parameter("size", 1f) 
        );

        public static readonly PrimitiveType
            RoundedCylinder = new PrimitiveType("RoundedCylinder(@pos, @par0, @par1, @par2)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                float radius = (float)parameters[0].GetValue();
                float smooth = (float)parameters[1].GetValue();
                float h = (float)parameters[2].GetValue();

                Vector2 d = new Vector2(p.XZ().magnitude - radius + smooth, Mathf.Abs(p.y) - h / 2f + smooth);
                return Mathf.Min(Mathf.Max(d.x, d.y), 0f) + d.Max(0f).magnitude - smooth;
            },
            Geometry.Exact,
            new FERM_Parameter("cylinderRadius", .5f),
            new FERM_Parameter("smoothing", .1f),
            new FERM_Parameter("length", 1f)
        );

        public static readonly PrimitiveType
            Cone = new PrimitiveType("Cone(@pos, @par0, @par1)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                float slope = (float)parameters[0].GetValue();
                float l = (float)parameters[1].GetValue();

                return Mathf.Max(FERM_Util.InfiniteCone(p, slope), -l - p.y);
            },
            Geometry.Exact,
            new FERM_Parameter("slope", 1f),
            new FERM_Parameter("length", 1f)
        );

        public static readonly PrimitiveType
            CappedCone = new PrimitiveType("CappedCone(@pos, @par1, @par0, @par2)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                float r1 = (float)parameters[1].GetValue();
                float r2 = (float)parameters[0].GetValue();
                float h = (float)parameters[2].GetValue();

                Vector2 q = new Vector2(p.XZ().magnitude, p.y);

                Vector2 k1 = new Vector2(r2, h / 2f);
                Vector2 k2 = new Vector2(r2 - r1, h);
                Vector2 ca = new Vector2(q.x - Mathf.Min(q.x, (q.y < 0f) ? r1 : r2), Mathf.Abs(q.y) - h / 2f);
                Vector2 cb = q - k1 + k2 * Mathf.Clamp(Vector3.Dot(k1 - q, k2) / k2.sqrMagnitude, 0f, 1f);
                float s = (cb.x < 0f && ca.y < 0.0) ? -1f : 1f;
                return s * Mathf.Sqrt(Mathf.Min(ca.sqrMagnitude, cb.sqrMagnitude));
            },
            Geometry.Exact,
            new FERM_Parameter("radius1", .2f),
            new FERM_Parameter("radius2", .5f),
            new FERM_Parameter("height", 1f)
        );

        public static readonly PrimitiveType
            RoundedCone = new PrimitiveType("RoundedCone(@pos, @par1, @par0, @par2)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                float r1 = (float)parameters[1].GetValue();
                float r2 = (float)parameters[0].GetValue();
                float h = (float)parameters[2].GetValue();

                Vector2 q = new Vector2(p.XZ().magnitude, p.y);
                h /= 2f;

                float b = (r1 - r2) / h;
                float a = Mathf.Sqrt(1f - b * b);
                float k = Vector2.Dot(q, new Vector2(-b, a));

                if(k < 0.0)
                    return q.magnitude - r1;
                if(k > a * h)
                    return (q - new Vector2(0f, h)).magnitude - r2;

                return Vector3.Dot(q, new Vector2(a, b)) - r1;
            },
            Geometry.Exact,
            new FERM_Parameter("radius1", .1f),
            new FERM_Parameter("radius2", .2f),
            new FERM_Parameter("height", 1f) 
        );
        
        public static readonly PrimitiveType
            Ellipsoid = new PrimitiveType("Ellipsoid(@pos, @par0)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                Vector3 r = (Vector3)parameters[0].GetValue();
                Vector3 rp = p.InverseScale(r);

                float k0 = rp.magnitude;
                rp = p.InverseScale(r);
                float k1 = rp.magnitude;
                return k0 * (k0 - 1f) / k1;
            },
            Geometry.BoundedConditonal,
            new FERM_Parameter("radius", new Vector3(.5f, 1f, .5f))
        );

        public static readonly PrimitiveType
            Arch = new PrimitiveType("Arch(@pos, @par0, @par1, @par2)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector3 size = (Vector3)parameters[0].GetValue();
                float radius = (float)parameters[1].GetValue();
                float height = (float)parameters[2].GetValue();

                Vector3 ps = pos - new Vector3(0f, (height - size.y) / 2f);
                Vector3 h = new Vector3(0f, height, size.z);
                return Mathf.Max(-FERM_Util.DF_Sphere(FERM_Util.Elongate(ps, h), radius), FERM_Util.DF_Box(pos, size));
            },
            Geometry.Bounded,
            new FERM_Parameter("size", new Vector3(2f, 2f, 2f)),
            new FERM_Parameter("radius", .5f),
            new FERM_Parameter("height", 1f)
        );

        public static readonly PrimitiveType
            Point = new PrimitiveType("length(@pos)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                return pos.magnitude;
            },
            Geometry.Exact
        );

        public readonly string distanceFunction;
        public readonly Geometry geometry;
        public readonly FERM_Parameter[] parameters;
        public readonly Func<Vector3, FERM_Parameter[], float> DF;

        private PrimitiveType(string distanceFunction, Func<Vector3, FERM_Parameter[], float> DF, Geometry geometry, params FERM_Parameter[] parameters) {
            this.distanceFunction = distanceFunction;
            this.DF = DF;
            this.geometry = geometry;
            this.parameters = parameters;
        }
    }

    private PrimitiveType GetShape(Type type) {
        System.Type shapeType = typeof(PrimitiveType);
        return (PrimitiveType)shapeType.GetField(type.ToString()).GetValue(null);
    }

    protected override string GetElementaryDF() {
       return GetShape(type).distanceFunction;
    }

    protected override FERM_Parameter[] GetElementaryParameters() {
        return GetShape(type).parameters;
    }

    public override int GetHashCode() {
        return 765 + (int)type + 297 * (int)unityTransformMode;
    }

    public override Geometry GetGeometry() {
        return GetShape(type).geometry;
    }

    protected override float ElementaryDF(Vector3 pos) {
        return GetShape(type).DF(pos, parameters);
    }
}
