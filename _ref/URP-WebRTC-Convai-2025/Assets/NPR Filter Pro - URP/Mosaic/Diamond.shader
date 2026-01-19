Shader "[QDY]NPR Filter Pro URP/Mosaic/Diamond" {
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

			float _Size;

			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;

				float2 ps = 10.0 / _Size;
				float2 coord = uv * ps;
				
				int dir = int(dot(frac(coord), half2(1, 1)) >= 1.0) + 2 * int(dot(frac(coord), half2(1, -1)) >= 0.0);
				
				coord = floor(coord);
				
				if (dir == 0) coord += half2(0, 0.5);
				if (dir == 1) coord += half2(0.5, 1);
				if (dir == 2) coord += half2(0.5, 0);
				if (dir == 3) coord += half2(1, 0.5);
				
				coord /= ps;
				
				float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, coord);
				return AreaShape(c, input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}