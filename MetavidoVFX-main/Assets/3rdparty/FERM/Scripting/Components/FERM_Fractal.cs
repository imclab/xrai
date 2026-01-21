using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FERM_Fractal : FERM_Shape3D {

    public Type type;

    public enum Type {
        Mandelbulb, SierpinskiTetrahedron, KochTetrahedron
    }

    private class FractalType {
        public static readonly FractalType
            Mandelbulb = new FractalType("Mandelbulb(@pos, @par0, @par1)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                int loop = (int)parameters[0].GetValue();
                float power = (float)parameters[1].GetValue();

                Vector3 z = p;
                float dr = 1f;
                float r = 0f;
                for(int i = 0; i < loop; i++) {
                    r = z.magnitude;
                    if(r > 10)
                        break;

                    // convert to polar coordinates
                    float theta = Mathf.Acos(z.y / r);
                    float phi = Mathf.Atan2(z.z, z.x) + Mathf.PI / 4f;

                    dr = (Mathf.Pow(r, power - 1f) * power * dr) + 1f;

                    // scale and rotate the point
                    float zr = Mathf.Pow(r, power);
                    theta *= power;
                    phi *= power;

                    // convert back to cartesian coordinates
                    z = zr * new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                    z += p;
                }
                return .5f * Mathf.Log(r) * r / dr;
            },
            new FERM_Parameter("max iterations", 20),
            new FERM_Parameter("power", 8f)
        );

        public static readonly FractalType
            KochTetrahedron = new FractalType("KochTetrahedron(@pos, @par0)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                int loop = (int)parameters[0].GetValue();

                Vector3 z = p;
                float s = 1f / 3f;
                float r = FERM_Util.DF_Tetrahedron(z, 1f);
                for(int n = 0; n < loop; n++) {
                    z = z.Mirror(new Vector3(.36f, 1f, -.61f));
                    z = z.Mirror(new Vector3(.36f, 1f, .61f));
                    z = z.Mirror(new Vector3(-.7f, 1f, 0f));
                    z = z.Mirror(new Vector3(0f, -1f, 0f), s);
                    z = new Quaternion(0f, 0.5f, 0f, 0.8660254f) * (z * 2f + new Vector3(0, s, 0));
                    float i = Mathf.Pow(2f, -n - 1) * FERM_Util.DF_Tetrahedron(z, 1f);
                    r = Mathf.Min(r, i);
                }
                return r;
            },
            new FERM_Parameter("iterations", 8)
        );

        public static readonly FractalType
            SierpinskiTetrahedron = new FractalType("SierpinskiTetrahedron(@pos, @par0)",
            (Vector3 p, FERM_Parameter[] parameters) => {
                int loop = (int)parameters[0].GetValue();

                Vector3 z = p;
                const float scale = 2f;
                const float offset = 3f;
                for(int n = 0; n < loop; n++) {
                    if(z.x + z.y < 0f)
                        z = z.XY(-z.YX());
                    if(z.x + z.z < 0f)
                        z = z.XZ(-z.ZX());
                    if(z.y + z.z < 0f)
                        z = z.ZY(-z.ZY());
                    z = (z * scale).Shift(-offset * (scale - 1f));
                }
                return z.magnitude * Mathf.Pow(scale, -loop);
            },
            new FERM_Parameter("iterations", 8)
        );

        public readonly string distanceFunction;
        public readonly FERM_Parameter[] parameters;
        public readonly Func<Vector3, FERM_Parameter[], float> DF;

        private FractalType(string distanceFunction, Func<Vector3, FERM_Parameter[], float> DF, params FERM_Parameter[] parameters) {
            this.distanceFunction = distanceFunction;
            this.DF = DF;
            this.parameters = parameters;
        }
    }

    private FractalType GetFractal(Type type) {
        System.Type shapeType = typeof(FractalType);
        return (FractalType)shapeType.GetField(type.ToString()).GetValue(null);
    }

    protected override string GetElementaryDF() {
        return GetFractal(type).distanceFunction;
    }

    protected override FERM_Parameter[] GetElementaryParameters() {
        return GetFractal(type).parameters;
    }

    public override int GetHashCode() {
        return 364 + (int)type + 499 * (int)unityTransformMode;
    }

    public override Geometry GetGeometry() {
        return Geometry.Complex;
    }

    protected override float ElementaryDF(Vector3 pos) {
        return GetFractal(type).DF(pos, parameters);
    }
}
