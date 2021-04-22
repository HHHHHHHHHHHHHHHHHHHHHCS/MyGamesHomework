Shader "My/S_Blit"
{
	Properties
	{
	}
	SubShader
	{

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct a2v
			{
				uint vertexID: SV_VERTEXID;
			};

			struct v2f
			{
				float4 pos: SV_POSITION;
				float2 uv: TEXCOORD0;
			};

			TEXTURE2D(_SrcTex);
			SAMPLER(sampler_LinearClamp);


			v2f vert(a2v v)
			{
				v2f o = (v2f)0;
				o.pos = GetFullScreenTriangleVertexPosition(v.vertexID);
				o.uv = GetFullScreenTriangleTexCoord(v.vertexID);
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				return SAMPLE_TEXTURE2D(_SrcTex, sampler_LinearClamp, i.uv);
			}
			ENDHLSL
		}
	}
}