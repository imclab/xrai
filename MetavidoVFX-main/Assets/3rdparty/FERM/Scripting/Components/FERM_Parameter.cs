using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FERM_Parameter {

    public string name;
    public string id;
    public Type type;
    public float[] value;

    public enum Type {
        Floating, Integer, Vector, Vec2, Quaternion, Axis
    }

    private class ParameterType {
        public static readonly ParameterType Floating = new ParameterType(
            delegate (Material mat, string id, object value) { mat.SetFloat(id, (float)value); },
            delegate (object value) { return new float[] { (float)value }; },
            delegate (float[] value) { return value[0]; },
            delegate (object o) { return o is float; },
            "float"
        );

        public static readonly ParameterType Integer = new ParameterType(
            delegate (Material mat, string id, object value) { mat.SetInt(id, (int)value); },
            delegate (object value) { return new float[] { (int)value }; },
            delegate (float[] value) { return (int)value[0]; },
            delegate (object o) { return o is int; },
            "int"
        );

        public static readonly ParameterType Vector = new ParameterType(
            delegate (Material mat, string id, object value) { mat.SetVector(id, (Vector3)value); },
            delegate (object value) {
                Vector3 v = (Vector3)value;
                return new float[] { v.x, v.y, v.z };
            },
            delegate (float[] value) {
                return new Vector3(value[0], value[1], value[2]);
            },
            delegate (object o) { return o is Vector3; },
            "float3"
        );

        public static readonly ParameterType Vec2 = new ParameterType(
            delegate (Material mat, string id, object value) { mat.SetVector(id, (Vector2)value); },
            delegate (object value) {
                Vector3 v = (Vector2)value;
                return new float[] { v.x, v.y};
            },
            delegate (float[] value) {
                return new Vector2(value[0], value[1]);
            },
            delegate (object o) { return o is Vector2; },
            "float2"
        );

        public static readonly ParameterType Quaternion = new ParameterType(
            delegate (Material mat, string id, object value) {
                Quaternion q = (Quaternion)value;
                mat.SetVector(id, FERM_Util.ToVector(q));
            },
            delegate (object value) {
                Quaternion q = (Quaternion)value;
                return new float[] { q.x, q.y, q.z, q.w };
            },
            delegate (float[] value) {
                return new Quaternion(value[0], value[1], value[2], value[3]);
            },
            delegate (object o) { return o is Quaternion; },
            "float4"
        );

        public static readonly ParameterType Axis = new ParameterType(
            delegate (Material mat, string id, object value) {
                int i = (int)value;
                mat.SetInt(id, i);
            },
            delegate (object value) {
                int i = (int)value;
                return new float[] { i };
            },
            delegate (float[] value) {
                return (Axis)(int)value[0];
            },
            delegate (object o) { return o is Axis; },
            "int"
        );

        public delegate void Apply(Material mat, string id, object value);
        public delegate float[] Set(object value);
        public delegate object Get(float[] value);
        public delegate bool Match(object o);

        private readonly Apply apply;
        public readonly Set set;
        public readonly Get get;
        public readonly Match match;
        public readonly string shaderType;

        private ParameterType(Apply apply, Set set, Get get, Match match, string shaderType) {
            this.apply = apply;
            this.set = set;
            this.get = get;
            this.match = match;
            this.shaderType = shaderType;
        }

        public void ApplyValue(Material mat, string id, float[] value) {
            this.apply(mat, id, this.get(value));
        }
    }

    public enum Axis {
        X = 0, Y = 1, Z = 2
    }

    private static ParameterType GetParameterType(Type type) {
        System.Type parameterType = typeof(ParameterType);
        return (ParameterType)parameterType.GetField(type.ToString()).GetValue(null);
    }

    private static Type GetType(object o) {
        foreach(Type type in Enum.GetValues(typeof(Type))) {
            if(GetParameterType(type).match(o))
                return type;
        }
        throw new Exception("Attempting to build parameter with unknown object type: " + o);
    }

    public FERM_Parameter(string name, object defaultValue) {
        this.name = name;
        this.id = null;
        this.type = GetType(defaultValue);
        this.value = GetParameterType(this.type).set(defaultValue);
    }

    public FERM_Parameter(FERM_Parameter template) {
        this.name = template.name;
        this.id = null;
        this.type = template.type;
        this.value = (float[])template.value.Clone();
    }

    public void ApplyToMaterial(Material mat) {
        GetParameterType(this.type).ApplyValue(mat, this.GetIdentifier(), this.value);
    }

    public void SetValue(object value) {
        this.value = GetParameterType(this.type).set(value);
    }

    public object GetValue() {
        return GetParameterType(this.type).get(this.value);
    }

    /// <summary>
    /// Returns string of cd shadercode that can be used to
    /// declare this parameter in that shader. This relies 
    /// on the id value that should have been set prior.
    /// </summary>
    public string GetShaderCodeDeclaration() {
        return GetShaderParamName() + " " + GetIdentifier() + ";\r\n";
    }
    
    public void SetGenericIdentifier(int index, int nbParams) {
        this.id = "par" + FERM_Util.NumString(index, nbParams);
    }

    public string GetIdentifier() {
        return this.id;
    }

    private string GetShaderParamName() {
        return GetParameterType(this.type).shaderType;
    }

    public static FERM_Parameter Clone(FERM_Parameter template) {
        System.Type paramType = template.GetType();
        System.Type[] typeParams = new System.Type[] { paramType };
        object[] constructorParams = new object[] { template };
        object toReturn = paramType.GetConstructor(typeParams).Invoke(constructorParams);
        return (FERM_Parameter)toReturn;
    }

    public bool Match(object o) {
        return GetParameterType(type).match(o);
    }
}
