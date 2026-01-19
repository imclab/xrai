Shader "[QDY]NPR Filter Pro URP/BayerDither" {
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

			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;

				// find bayer matrix entry based on fragment position
				float4x4 bayerIndex = float4x4(
					float4(00.0/16.0, 12.0/16.0, 03.0/16.0, 15.0/16.0),
					float4(08.0/16.0, 04.0/16.0, 11.0/16.0, 07.0/16.0),
					float4(02.0/16.0, 14.0/16.0, 01.0/16.0, 13.0/16.0),
					float4(10.0/16.0, 06.0/16.0, 09.0/16.0, 05.0/16.0));
				float2 coord = uv * _BlitTexture_TexelSize.zw;
				float bayer = bayerIndex[int(coord.x) % 4][int(coord.y) % 4];

				float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
				float4 fc = float4(
					step(bayer, col.r),
					step(bayer, col.g),
					step(bayer, col.b),
					col.a);
				return AreaShape(fc, input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}