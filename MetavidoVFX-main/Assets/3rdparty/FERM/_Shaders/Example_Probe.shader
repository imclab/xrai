Shader "FERM/FERM_0000"
{


/*
 * -------------------------------------------------------------------------
 * ----------------------------- PROPERTIES --------------------------------
 * -------------------------------------------------------------------------
 */

 

Properties
{
    [HideInInspector] [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling", Int) = 2
	[HideInInspector] [Toggle][KeyEnum(Off, On)] _ZWrite("ZWrite", Float) = 1
     
    [Header(Raymarching)]
    _Qx("Quality factor", Range(-1,1)) = 0 
    _Qy("Cutoff factor", Range(-1,1)) = 0
	_Qz("Oversample factor", Range(0,1)) = 0
    _Qr("Render radius", Float) = 10000

	[Header(Surface)]
	_MainColor("Color", Color) = (1, 1, 1, 1)
    _OcclusionColor("OcclusionColor", Color) = (0, 0, 0, 1)
    _MainTex ("Texture", 2D) = "white" {}
    [Enum(XZ,0,XY,1,YZ,2,Sphere,3,Cylinder,4,Radial,5)] _UVMode ("UV mode", Int) = 0
}


SubShader
{

Tags
{
    "RenderType" = "Opaque"
    "Queue" = "Geometry"
    "DisableBatching" = "True"
}

Cull [_Cull]
CGINCLUDE

//#options
#define USE_RAYMARCHING_DEPTH
#define SUPERSAMPLING_1X

//#

#include "Include/Common.cginc"

/*
 * -------------------------------------------------------------------------
 * ------------------------- DISTANCE ESTIMATOR ----------------------------
 * -------------------------------------------------------------------------
 */
 
uniform float3 _t_position; 
uniform float4 _t_rotation;
uniform float _t_scale;  

//#parameters
float par00;
float par01;
float par02;
float par03;
float par04;
float3 par05;
float4 par06;
float par07;
int par08;
float par09;
float3 par10;
float4 par11;
float par12;

//#

//#helpers

//#

#define DISTANCE_FUNCTION DistanceFunction
inline float DistanceFunction(float3 pos, float3 dir)
{
    //#function
return DeformedMix((Sphere(InverseTransform(par05, par06, par07, pos), par04) * par07), (Mandelbulb(InverseTransform(par10, par11, par12, pos), par08, par09) * par12), par00, par01, par02, par03);
//#
}


/*
 * -------------------------------------------------------------------------
 * ---------------------------- POST EFFECT --------------------------------
 * -------------------------------------------------------------------------
 */

#define POST_EFFECT PostEffect
#define PostEffectOutput float4

fixed4 _MainColor;
fixed4 _OcclusionColor;
sampler2D _MainTex;
float4 _MainTex_ST;

inline void PostEffect(RaymarchInfo ray, inout PostEffectOutput o)
{
	float ao = GoldInverse(1.0 * ray.loop / ray.maxLoop);
    float2 uv = TRANSFORM_TEX(GetUV(ray), _MainTex);
    o.rgb = tex2D(_MainTex, uv);
    o.rgb *= lerp(_OcclusionColor, _MainColor, ao);
}


ENDCG

Pass
{
    Tags { "LightMode" = "ForwardBase" }

    ZWrite [_ZWrite]

    CGPROGRAM
    #include "Include/ForwardBaseUnlit.cginc"
    #pragma target 3.0
    #pragma vertex Vert
    #pragma fragment Frag
    #pragma multi_compile_instancing
    #pragma multi_compile_fog
    #pragma multi_compile_fwdbase
    ENDCG
}


} Fallback Off 

}