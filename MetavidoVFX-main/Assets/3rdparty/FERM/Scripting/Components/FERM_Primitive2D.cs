using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FERM_Primitive2D : FERM_Shape2D {

    public Type type;

    public enum Type {
        Circle, Triangle, Rectangle, Polygon, Rouleaux, Spiral, Sector
    }

    private class Primitive2DType {
         public static readonly Primitive2DType
            Circle = new Primitive2DType("Circle(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector2 p = FERM_Util.Flat(pos);
                float radius = (float)parameters[0].GetValue();
                float d = p.magnitude - radius;
                return FERM_Util.FlatDistance(pos, d); 
            },
            Geometry.Exact,
            new FERM_Parameter("radius", .5f)
        );

         public static readonly Primitive2DType
            Triangle = new Primitive2DType("Triangle(@pos, @par0, @par1, @par2)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                Vector2 a = (Vector2)parameters[0].GetValue();
                Vector2 b = (Vector2)parameters[1].GetValue();
                Vector2 c = (Vector2)parameters[2].GetValue();
                return FERM_Util.DF_Triangle(p, a.XZ(), b.XZ(), c.XZ());
            },
            Geometry.Exact,
            new FERM_Parameter("point1", new Vector2(0f, 0f)),
            new FERM_Parameter("point2", new Vector2(1f, 0f)),
            new FERM_Parameter("point3", new Vector2(0f, 1f))
        );

        public static readonly Primitive2DType
            Rectangle = new Primitive2DType("Quad(@pos, @par0, @par1)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector2 p = FERM_Util.Flat(pos);
                Vector2 size = (Vector2)parameters[0].GetValue();
                float round = (float)parameters[1].GetValue();
                Vector2 delta = FERM_Util.Shift(FERM_Util.Abs(p) - size / 2f, round);
                float d = FERM_Util.Max(delta, 0f).magnitude - round;
                return FERM_Util.FlatDistance(pos, d);
            },
            Geometry.Exact,
            new FERM_Parameter("size", Vector2.one),
            new FERM_Parameter("rounding", .1f)
        );

        public static readonly Primitive2DType
            Polygon = new Primitive2DType("Polygon(@pos, @par0, @par1)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector2 p = FERM_Util.Flat(pos);
                float radius = (float)parameters[0].GetValue();
                int sides = (int)parameters[1].GetValue();
                float ang = Mathf.PI / sides;
                float theta = (Mathf.Atan2(p.y, p.x) + Mathf.PI) % (ang * 2f) - ang;
                float d = (p.magnitude * Mathf.Cos(theta)) - (radius * Mathf.Cos(ang));
                return FERM_Util.FlatDistance(pos, d);
            },
            Geometry.Exact,
            new FERM_Parameter("radius", .5f),
            new FERM_Parameter("sides", 3)
        );

        public static readonly Primitive2DType
            Rouleaux = new Primitive2DType("Rouleaux(@pos, @par0, @par1)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector2 p = FERM_Util.Flat(pos);
                float radius = (float)parameters[0].GetValue();
                int sides = (int)parameters[1].GetValue();
                float ang = Mathf.PI / sides;
                float theta = (Mathf.Atan2(p.y, p.x) + Mathf.PI) % (ang * 2f) - ang;
                Vector2 c = p.magnitude * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) + new Vector2(radius / 2f, 0f);
                float d = c.magnitude - radius;
                return FERM_Util.FlatDistance(pos, d);
            },
            Geometry.Exact,
            new FERM_Parameter("radius", .5f),
            new FERM_Parameter("sides", 3)
        );

        public static readonly Primitive2DType
            Spiral = new Primitive2DType("Spiral(@pos, @par0, @par1, @par2)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector2 p = FERM_Util.Flat(pos);
                float spacing = (float)parameters[0].GetValue();
                float thickness = (float)parameters[1].GetValue();
                float radius = (float)parameters[2].GetValue();
                float theta = Mathf.Atan2(p.y, p.x);
                float r = (p.magnitude + spacing * theta / (2f * Mathf.PI)) % spacing;
                float d = Mathf.Max(Mathf.Min(r, spacing - r) - (thickness / 2f), p.magnitude - radius);
                return FERM_Util.FlatDistance(pos, d);
            },
            Geometry.Exact,
            new FERM_Parameter("spacing", .2f),
            new FERM_Parameter("thickness", .1f),
            new FERM_Parameter("radius", 5f)
        );

        public static readonly Primitive2DType
            Sector = new Primitive2DType("Sector(@pos, @par0, @par1)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                Vector2 p = FERM_Util.Flat(pos);
                float radius = (float)parameters[0].GetValue();
                float angle = (float)parameters[1].GetValue() * Mathf.Deg2Rad;
                float theta = Mathf.Abs(Mathf.Atan2(p.y, p.x));
                if(Mathf.Abs(theta) < angle)
                    return p.magnitude - radius;
                theta -= angle;
                p = p.magnitude * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
                float d = FERM_Util.DF_Line(p, radius);
                return FERM_Util.FlatDistance(pos, d);
            },
            Geometry.Exact,
            new FERM_Parameter("radius", .5f),
            new FERM_Parameter("angle", 45f)
        );

        public readonly string distanceFunction;
        public Func<Vector3, FERM_Parameter[], float> DF;
        public readonly Geometry geometry;
        public readonly FERM_Parameter[] parameters;

        private Primitive2DType(string distanceFunction, Func<Vector3, FERM_Parameter[], float> DF, Geometry geometry, params FERM_Parameter[] parameters) {
            this.distanceFunction = distanceFunction;
            this.DF = DF;
            this.geometry = geometry;
            this.parameters = parameters;
        }
    }

    private Primitive2DType GetShape(Type type) {
        System.Type shapeType = typeof(Primitive2DType);
        return (Primitive2DType)shapeType.GetField(type.ToString()).GetValue(null);
    }

    protected override string GetElementaryDF() {
        return GetShape(type).distanceFunction;
    }

    protected override FERM_Parameter[] GetElementaryParameters() {
        return GetShape(type).parameters;
    }

    public override int GetHashCode() {
        return 983 + (int)type + 297 * (int)unityTransformMode;
    }

    public override Geometry GetGeometry() {
        return GetShape(type).geometry;
    }

    protected override float ElementaryDF(Vector3 pos) {
        return GetShape(type).DF(pos, parameters);
    }
}
