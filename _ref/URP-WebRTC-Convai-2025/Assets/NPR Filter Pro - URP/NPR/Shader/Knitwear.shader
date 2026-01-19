Shader "[QDY]NPR Filter Pro URP/Knitwear" {
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

			sampler2D _KnitwearTex;
			float _KnitwearShear, _KnitwearDivision, _KnitwearAspect;

			void KnitwearCoordinate (inout float2 uv, out float2 cell, half shear)
			{
				float offset = distance(frac(uv.x), 0.5) * shear;
				uv.y += offset;

				cell = floor(uv * float2(2.0, 1.0));
				cell += float2(0.5, 0.5);
				cell *= float2(0.5, 1.0);
			}
			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;
				float2 uvNew = uv;

				float2 scale = _KnitwearDivision / float2(_KnitwearAspect, 1.0);
				uv *= scale;

				KnitwearCoordinate(uv, uvNew, _KnitwearShear);
				uvNew /= scale;

				float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvNew);
				float4 kc = tex2D(_KnitwearTex, uv) * 1.2;
				return AreaShape(c * kc, input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}