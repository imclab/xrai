Shader "[QDY]NPR Filter Pro URP/Tiles" {
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

			float _NumTiles, _Threshhold;
			float3 _EdgeColor;

			float4 frag (Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				float2 uv = input.texcoord;

				float sz = 1.0 / _NumTiles;
				float2 base = uv - fmod(uv, sz.xx);
				float2 center = base + (sz / 2.0).xx;
				float2 st = (uv - base) / sz;
				float4 c1 = 0.0;
				float4 c2 = 0.0;
				float4 io = float4((1.0 - _EdgeColor), 1.0);
				if (st.x > st.y) { c1 = io; }
				float threshholdB =  1.0 - _Threshhold;
				if (st.x > threshholdB)  { c2 = c1; }
				if (st.y > threshholdB)  { c2 = c1; }
				float4 bottom = c2;
				c1 = 0.0;
				c2 = 0.0;
				if (st.x > st.y)  { c1 = io; }
				if (st.x < _Threshhold)  { c2 = c1; }
				if (st.y < _Threshhold)  { c2 = c1; }
				return AreaShape(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, center) + c2 - bottom, input);
			}
			ENDHLSL
		}
	}
	FallBack Off
}