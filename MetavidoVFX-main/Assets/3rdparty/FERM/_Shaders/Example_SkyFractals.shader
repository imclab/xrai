Shader "FERM/Example_SkyFractals"
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
    [Enum(XZ,0,XY,1,YZ,2,Sphere,3,Cylinder,4)] _UVMode ("UV mode", Int) = 0
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
#define SKYBOX_MODE
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
float3 par00;
float4 par01;
float par02;
float3 par03;
float4 par04;
float par05;
float3 par06;
float par07;
float3 par08;
float par09;
float3 par10;
float par11;
int par12;
float3 par13;
float3 par14;
float3 par15;
float par16;

//#

//#helpers

inline float hlp0(float3 pos)
{
    for(int i = 0; i < par12; i++) {
        pos = InverseTransform(par03, par04, par05, Mirror(Mirror(Mirror(pos, par10, par11), par08, par09), par06, par07));
    }
    float toReturn = length(pos);
    for(i = 0; i < par12; i++) {
        toReturn = (toReturn * par05);
    }
    return toReturn;
}
//#

#define DISTANCE_FUNCTION DistanceFunction
inline float DistanceFunction(float3 pos, float3 dir)
{
    //#function
return hlp0((Modulo(Twist(pos, par15, par16), par14) - par13));
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