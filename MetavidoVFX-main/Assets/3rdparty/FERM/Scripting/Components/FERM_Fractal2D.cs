using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FERM_Fractal2D : FERM_Shape2D {

    public Type type;

    public enum Type {
        Mandelbrot, Julia
    }

    private class Fractal2DType {

        public static readonly Fractal2DType
            Mandelbrot = new Fractal2DType("Mandelbrot(@pos, @par0)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                int iterations = (int)parameters[0].GetValue();

                Vector2 z = new Vector2();
                Vector2 dz = new Vector2();
                Vector2 p = FERM_Util.Flat(pos);

                float m2 = 1f;
                for(int i = 0; i < iterations; i++) {
                    dz = (2f * z.CMul(dz)).Shift(1f);
                    z = z.CMul(z) + p;

                    m2 = z.sqrMagnitude;
                    if(m2 > 1e5f)
                        break;
                }

                return Mathf.Sqrt(m2 / dz.sqrMagnitude) * .5f * Mathf.Log(m2);
            },
            new FERM_Parameter("max iterations", 20)
        );

        public static readonly Fractal2DType
            Julia = new Fractal2DType("Julia(@pos, @par0, @par1)",
            (Vector3 pos, FERM_Parameter[] parameters) => {
                int iterations = (int)parameters[0].GetValue();
                Vector2 z = (Vector2)parameters[1].GetValue();
                Vector2 p = FERM_Util.Flat(pos);

                Vector2 dz = new Vector2(1f, 0f);
                float m2 = 1f;
                for(int i = 0; i < iterations; i++) {
                    dz = (2f * z.CMul(dz)).Shift(1f);
                    z = z.CMul(z) + p;
                    m2 = z.sqrMagnitude;
                    if(m2 > 1e+5)
                        break;

                }

                float d = Mathf.Sqrt(m2 / dz.sqrMagnitude) * .5f * Mathf.Log(m2);
                return FERM_Util.FlatDistance(pos, d);
            },
            new FERM_Parameter("max iterations", 20),
            new FERM_Parameter("z", Vector2.zero)
        );

        public readonly string distanceFunction;
        public readonly Func<Vector3, FERM_Parameter[], float> DF;
        public readonly FERM_Parameter[] parameters;

        private Fractal2DType(string distanceFunction, Func<Vector3, FERM_Parameter[], float> DF, params FERM_Parameter[] parameters) {
            this.distanceFunction = distanceFunction;
            this.DF = DF;
            this.parameters = parameters;
        }
    }

    private Fractal2DType GetFractal(Type type) {
        System.Type shapeType = typeof(Fractal2DType);
        return (Fractal2DType)shapeType.GetField(type.ToString()).GetValue(null);
    }

    protected override string GetElementaryDF() {
        return GetFractal(type).distanceFunction;
    }

    protected override FERM_Parameter[] GetElementaryParameters() {
        return GetFractal(type).parameters;
    }

    public override int GetHashCode() {
        return 563 + (int)type + 499 * (int)unityTransformMode;
    }

    public override Geometry GetGeometry() {
        return Geometry.Complex;
    }

    protected override float ElementaryDF(Vector3 pos) {
        return GetFractal(type).DF(pos, parameters);
    }
}
