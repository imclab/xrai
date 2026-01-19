Shader "[QDY]NPR Filter Pro URP/Brick" {
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

			float _Size, _GridShadow, _CircleSize, _CircleHard;

			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float sz = _Size;
				float2 coord = input.texcoord * _BlitTexture_TexelSize.zw;
				float2 middle = floor(coord * sz + 0.5) / sz;

				float3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, middle / _BlitTexture_TexelSize.zw).rgb;

				float dis = abs(distance(coord, middle) * sz * _CircleSize - 0.6);
				c *= smoothstep(0.1, 0.05, dis) * dot(_CircleHard, normalize(coord - middle)) * 0.5 + 1.0;

				// side shadow
				float2 delta = abs(coord - middle) * sz * _GridShadow;
				float md = max(delta.x, delta.y);
				c *= 0.8 + smoothstep(0.95, 0.8, md) * 0.2;
				return AreaShape(float4(c, 1.0), input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}