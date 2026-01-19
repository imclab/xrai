Shader "[QDY]NPR Filter Pro URP/OneBit" {
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
			#pragma multi_compile _ NFP_OrigColor
			#include "Common.hlsl"

			float4 _ColorA, _ColorB;
			float _DitherSize, _Threshold, _Scale, _ColorBit;
			int _ColorDepth;

			int ditherPattern (int2 uv) {
				const int pattern[] = {
					-4, +0, -3, +1,
					+2, -2, +3, -1,
					-3, +1, -4, +0,
					+3, -1, +2, -2
				};
				int x = uv.x % 4;
				int y = uv.y % 4;
				return pattern[y * 4 + x] * _DitherSize;
			}
			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float4 orig = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
				int3 c = int3(round(orig.rgb * _ColorBit));
				int2 uvInt = int2(input.texcoord * _BlitTexture_TexelSize.zw);
				uvInt /= _Scale;
				c += ditherPattern(uvInt).xxx;
#if NFP_OrigColor
				c >>= (8 - _ColorDepth);
				return AreaShape(float4(float3(c) / float(1 << _ColorDepth), 1.0), input);
#else
				float avg = float(c.x + c.y + c.z) / 3.0;
				float4 result;
				if (avg < _Threshold)
					result = _ColorA;
				else
					result = _ColorB;
				return AreaShape(result, input);
#endif
			}
			ENDHLSL
		}
	}
	FallBack Off
}