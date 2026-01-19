Shader "[QDY]NPR Filter Pro URP/WaterColor" {
	Properties {
		_WobbleTex    ("Wobbing", 2D) = "grey" {}
		_WobbleScale  ("Wobbing Scale", Float) = 1
		_WobblePower  ("Wobbing Power", Float) = 1
		_EdgeSize     ("Edge Size", Float) = 1
		_EdgePower    ("Edge Power", Float) = 1
		_PaperTex     ("Paper", 2D) = "grey" {}
		_PaperPower   ("Paper Power", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		Cull Off ZWrite Off ZTest Always Fog { Mode Off }
		HLSLINCLUDE
			#include "Common.hlsl"
			float4 ColorBlend (float4 c, float d)  { return c - (c - c * c) * (d - 1); }
		ENDHLSL
		Pass {   // pass 0, wobble pass
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			sampler2D _WobbleTex;
			float _WobbleScale, _WobblePower;

			float4 frag (Varyings input) : SV_TARGET
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;

				float aspect = _ScreenParams.x / _ScreenParams.y;
				float2 uv1 = uv * float2(aspect, 1) * _WobbleScale;

				float2 wobb = tex2D(_WobbleTex, uv1).wy * 2.0 - 1.0;
				return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + wobb * _WobblePower);
			}
			ENDHLSL
		}
		Pass {   // pass 1, edge darkening pass
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			float _EdgeSize, _EdgePower;

			float4 frag (Varyings input) : SV_TARGET
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;

				float2 offset = _BlitTexture_TexelSize.xy * _EdgeSize;
				float4 l = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-offset.x, 0));
				float4 r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(+offset.x, 0));
				float4 b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(           0, -offset.y));
				float4 t = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(           0, +offset.y));
				float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

				float4 grad = abs(r - l) + abs(b - t);
				float intens = saturate(0.333 * (grad.x + grad.y + grad.z));
				float d = _EdgePower * intens + 1;
				return ColorBlend(c, d);
			}
			ENDHLSL
		}
		Pass {   // pass 2, paper layer pass
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			#pragma multi_compile _ NPR_Circle
			#pragma multi_compile _ NPR_Reverse

			sampler2D _PaperTex, _Global_OrigScene;
			float _PaperPower;

			float4 frag (Varyings input) : SV_TARGET
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;

				float4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
				float paper = tex2D(_PaperTex, uv).x;
				float d = _PaperPower * (paper - 0.5) + 1;

				float4 orig = tex2D(_Global_OrigScene, uv);
				return AreaShapeColor(ColorBlend(src, d), uv, orig);
			}
			ENDHLSL
		}
	}
	Fallback Off
}
