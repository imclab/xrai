#ifndef COMMON_HLSL
#define COMMON_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

float4 _BlitTexture_TexelSize, _CircleXYWH;

float4 Private_AreaShape(float4 fx, float2 uv, float4 orig)
{
#ifdef NPR_Circle
	float ratio = (_BlitTexture_TexelSize.z / _BlitTexture_TexelSize.w);
	float2 uv2 = float2(uv.x * ratio, uv.y);
	float2 center = float2(_CircleXYWH.x * ratio, _CircleXYWH.y);
	float dist = distance(uv2, center);
	float f = smoothstep(_CircleXYWH.z, _CircleXYWH.z + _CircleXYWH.w, dist);
#ifdef NPR_Reverse
	f = 1.0 - f;
#endif
	return lerp(fx, orig, f);
#endif
	return fx;
}

float4 AreaShape(float4 fx, Varyings input)
{
	float4 orig = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
	return Private_AreaShape(fx, input.texcoord, orig);
}
float4 AreaShapeColor(float4 fx, float2 uv, float4 orig)
{
	return Private_AreaShape(fx, uv, orig);
}

#endif