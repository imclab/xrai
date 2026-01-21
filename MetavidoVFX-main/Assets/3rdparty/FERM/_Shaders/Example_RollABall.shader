Shader "FERM/Example_RollABall"
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
	
	[Header(Standard shader)]
	_ShadowLoop("Shadow Loop", Range(1, 100)) = 30
    _ShadowMinDistance("Shadow Minimum Distance", Range(0.001, 0.1)) = 0.01
    _ShadowExtraBias("Shadow Extra Bias", Range(0.0, 1.0)) = 0.0
    
    [Header(Surface)]
	_MainColor("Color", Color) = (1, 1, 1, 1)
    _MainTex ("Texture", 2D) = "white" {}
    [Enum(XZ,0,XY,1,YZ,2,Sphere,3,Cylinder,4)] _UVMode ("UV mode", Int) = 0
	_SurfaceMetallic("Metallic", Range(0,1)) = .5 
	_SurfaceSmoothness("Smoothness", Range(0,1)) = .5 
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
float3 par04;
float3 par05;
float3 par06;
float par07;
float par08;
float par09;
float3 par10;
float4 par11;
float par12;
float3 par13;
float3 par14;
float3 par15;
float4 par16;
float par17;
float3 par18;
float3 par19;
float4 par20;
float par21;
float3 par22;
float3 par23;
float4 par24;
float par25;
float3 par26;
float3 par27;
float4 par28;
float par29;
float3 par30;
float3 par31;
float4 par32;
float par33;
int par34;
float par35;
float3 par36;
float4 par37;
float par38;

//#

//#helpers

//#

#define DISTANCE_FUNCTION DistanceFunction
inline float DistanceFunction(float3 pos, float3 dir) 
{
	//#function
return DeformedMix(SmoothDifference((Circle(Elongate(InverseTransform(par10, par11, par12, Repeat(Bend(pos, par05, par06, par07), par04)), par13), par09) * par12), Union(Union(Difference((Box(InverseTransform(par15, par16, par17, Repeat(Bend(pos, par05, par06, par07), par04)), par14) * par17), (Box(InverseTransform(par19, par20, par21, Repeat(Bend(pos, par05, par06, par07), par04)), par18) * par21)), (Box(InverseTransform(par23, par24, par25, Repeat(Bend(pos, par05, par06, par07), par04)), par22) * par25)), Union((Box(InverseTransform(par27, par28, par29, Repeat(Bend(pos, par05, par06, par07), par04)), par26) * par29), (Box(InverseTransform(par31, par32, par33, Repeat(Bend(pos, par05, par06, par07), par04)), par30) * par33))), par08), (Mandelbulb(InverseTransform(par36, par37, par38, Repeat(Bend(pos, par05, par06, par07), par04)), par34, par35) * par38), par00, par01, par02, par03);
//#
}


/*
 * -------------------------------------------------------------------------
 * ---------------------------- POST EFFECT --------------------------------
 * -------------------------------------------------------------------------
 */

#define POST_EFFECT PostEffect

fixed4 _MainColor;
sampler2D _MainTex;
float4 _MainTex_ST;

fixed4 _SurfaceColor;
half _SurfaceMetallic, _SurfaceSmoothness;

#define PostEffectOutput SurfaceOutputStandard 
inline void PostEffect(RaymarchInfo ray, inout PostEffectOutput o)
{
    o.Occlusion = GoldInverse(1.0 * ray.loop / ray.maxLoop);
    float2 uv = TRANSFORM_TEX(GetUV(ray), _MainTex);
    o.Albedo = _MainColor * tex2D(_MainTex, uv);
	o.Metallic = _SurfaceMetallic;
	o.Smoothness = _SurfaceSmoothness;
}


ENDCG

Pass
{
    Tags { "LightMode" = "ForwardBase" }

    ZWrite [_ZWrite]

    CGPROGRAM
    #include "Include/ForwardBaseStandard.cginc"
    #pragma target 3.0
    #pragma vertex Vert
    #pragma fragment Frag
    #pragma multi_compile_instancing
    #pragma multi_compile_fog
    #pragma multi_compile_fwdbase
    ENDCG
}


Pass
{
    Tags { "LightMode" = "ShadowCaster" }

    CGPROGRAM
    #include "Include/ShadowCaster.cginc"
    #pragma target 3.0
    #pragma vertex Vert
    #pragma fragment Frag
    #pragma fragmentoption ARB_precision_hint_fastest
    #pragma multi_compile_shadowcaster
    ENDCG
}


} Fallback Off 

}