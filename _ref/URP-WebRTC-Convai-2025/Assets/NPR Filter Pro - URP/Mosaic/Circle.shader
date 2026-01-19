Shader "[QDY]NPR Filter Pro URP/Mosaic/Circle" {
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

			float4 _BackgroundColor, _Params;

			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;

				float ratio = _ScreenParams.y / _ScreenParams.x;
				uv.x = uv.x / ratio;

				float interval = _Params.y;
				float scale = 1.0 / _Params.x;
				float2 coord = half2(interval * floor(uv.x / (scale * interval)), interval * floor(uv.y / (scale * interval)));

				float2 center = coord * scale + scale * 0.5;
				float dist = length(uv - center) * _Params.x;
				center.x *= ratio;
				float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, center);

				if (dist > _Params.z)
					c = _BackgroundColor;
				return AreaShape(c, input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}