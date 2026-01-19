Shader "[QDY]NPR Filter Pro URP/BlackWhiteScary" {
	Properties {}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		Cull Off ZWrite Off ZTest Always Fog { Mode Off }
		Pass {
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			#pragma multi_compile _ NPR_Circle
			#pragma multi_compile _ NPR_Reverse
			#include "Common.hlsl"

			half _Speed, _Zigzag, _Noisy;

			float mod (float a, float b) { return a - b * floor(a / b); }
			float rand (float x)         { return frac(sin(x) * 43758.5453); }
			float tri (float x)          { return abs(1.0 - mod(abs(x), 2.0)) * 2.0 - 1.0; }
			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float t = floor(_Time.y * _Speed) / _Speed;

				float2 p = input.texcoord;
				p += float2(tri(p.y * rand(t) * 4.0) * rand(t * 1.9) * _Zigzag,
							tri(p.x * rand(t * 3.4) * 4.0) * rand(t * 2.1) * _Zigzag);
				p += float2(rand(p.x * 3.1 + p.y * 8.7) * _Noisy, rand(p.x * 1.1 + p.y * 6.7) * _Noisy);

				float3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
				float3 edges = 1.0 - (c / SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, p).rgb);
				return AreaShape(half4(length(edges).xxx, 1.0), input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
