Shader "FERM/Example_Title"
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
float3 par01;
float par02;
float3 par03;
float4 par04;
float par05;
float3 par06;
float par07;
float3 par08;
float4 par09;
float par10;
float3 par11;
float par12;
float3 par13;
float4 par14;
float par15;
float par16;
float par17;
float3 par18;
float3 par19;
float4 par20;
float par21;
float par22;
float3 par23;
float3 par24;
float4 par25;
float par26;
float par27;
float3 par28;
float4 par29;
float par30;
float par31;
float3 par32;
float par33;
float3 par34;
float4 par35;
float par36;
float3 par37;
float par38;
float par39;
float3 par40;
float4 par41;
float par42;
float3 par43;
float par44;
float3 par45;
float4 par46;
float par47;
int par48;
float par49;
float3 par50;
float4 par51;
float par52;
float par53;
float par54;
float3 par55;
float4 par56;
float par57;
float4 par58;
int par59;
float par60;
float3 par61;
float par62;
float3 par63;
float par64;
float par65;
float3 par66;
float4 par67;
float par68;
float par69;
float3 par70;
float4 par71;
float par72;
float par73;
float3 par74;
float4 par75;
float par76;
float par77;
float3 par78;
float4 par79;
float par80;

//#

//#helpers

inline float hlp0(float3 pos)
{
    for(int i = 0; i < par59; i++) {
        pos = (Mirror(Mirror(pos, par63, par64), par61, par62) / par60);
    }
    float toReturn = Capsule(InverseRotate(par58, pos), par53, par54);
    for(i = 0; i < par59; i++) {
        toReturn = (toReturn * par60);
    }
    return toReturn;
}
//#

#define DISTANCE_FUNCTION DistanceFunction
inline float DistanceFunction(float3 pos, float3 dir)
{
    //#function
return Union(Union(Union(SmoothUnion(SmoothUnion((RoundedBox(InverseTransform(par03, par04, par05, pos), par01, par02) * par05), (RoundedBox(InverseTransform(par08, par09, par10, pos), par06, par07) * par10), par00), (RoundedBox(InverseTransform(par13, par14, par15, pos), par11, par12) * par15), par00), SmoothDifference((Box(InverseTransform(par19, par20, par21, pos), par18) * par21), SmoothDifference((Box(InverseTransform(par24, par25, par26, pos), par23) * par26), (Sphere(InverseTransform(par28, par29, par30, pos), par27) * par30), par22), par17)), Union(Intersect(SmoothUnion(SmoothUnion((RoundedBox(InverseTransform(par34, par35, par36, pos), par32, par33) * par36), (Arch(InverseTransform(par40, par41, par42, pos), par37, par38, par39) * par42), par31), (RoundedBox(InverseTransform(par45, par46, par47, pos), par43, par44) * par47), par31), (Mandelbulb(InverseTransform(par50, par51, par52, pos), par48, par49) * par52)), (hlp0(InverseTransform(par55, par56, par57, pos)) * par57))), Union(Union((Sphere(InverseTransform(par66, par67, par68, pos), par65) * par68), (Sphere(InverseTransform(par70, par71, par72, pos), par69) * par72)), Union((Sphere(InverseTransform(par74, par75, par76, pos), par73) * par76), (Sphere(InverseTransform(par78, par79, par80, pos), par77) * par80))));
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