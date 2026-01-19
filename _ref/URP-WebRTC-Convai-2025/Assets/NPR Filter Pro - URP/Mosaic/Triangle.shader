Shader "[QDY]NPR Filter Pro URP/Mosaic/Triangle" {
	Properties {}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		Cull Off ZWrite Off ZTest Always
		Pass {
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			#pragma multi_compile _ NPR_Circle
			#pragma multi_compile _ NPR_Reverse
			#include "Common.hlsl"

			float _Size, _Ratio, _ScaleX, _ScaleY;

			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;

				float2 ps = _Size * float2(_ScaleX, _ScaleY / _Ratio);
				float2 coord = floor(uv * ps) / ps;
				
				uv -= coord;
				uv *= ps;
				
				coord += float2(
					step(1.0 - uv.y, uv.x) / (2.0 * ps.x),
					step(uv.x, uv.y) / (2.0 * ps.y)
				);
				float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, coord);
				return AreaShape(c, input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}