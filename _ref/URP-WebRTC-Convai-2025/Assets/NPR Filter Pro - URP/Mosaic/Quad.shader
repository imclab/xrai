Shader "[QDY]NPR Filter Pro URP/Mosaic/Quad" {
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

				float scl = 1.0 / _Size;
				float2 newUv = half2(scl * _ScaleX * floor(uv.x / (scl *_ScaleX)), (scl * _Ratio *_ScaleY) * floor(uv.y / (scl *_Ratio * _ScaleY)));
				float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, newUv);
				return AreaShape(c, input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}