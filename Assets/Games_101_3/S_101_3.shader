Shader "Games/S_101_3"
{
	Properties
	{
		_Color ("Color", Color) = (0.4,0.4,0.2,1)
		_AlbedoTex ("Albedo Texture", 2D) = "white" {}
		[Toggle] _Use_Displacement("Displacement",int) = 0
		_NormalTex ("Normal Texture", 2D) = "bump" {}
	}
	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
		}
		LOD 100

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_local _ _USE_DISPLACEMENT_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct a2v
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
			};

			struct v2f
			{
				float4 hPos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 TBN0 : TEXCOORD1;
				float4 TBN1 : TEXCOORD2;
				float4 TBN2 : TEXCOORD3;
			};

			CBUFFER_START(UnityPerMaterial)
			half4 _Color;
			CBUFFER_END

			TEXTURE2D(_AlbedoTex);
			SAMPLER(sampler_AlbedoTex);
			TEXTURE2D(_NormalTex);
			SAMPLER(sampler_NormalTex);


			v2f vert(a2v v)
			{
				v2f o;

				#if _USE_DISPLACEMENT_ON
					v.vertex.xyz += v.normal.xyz * UnpackNormal(SAMPLE_TEXTURE2D_LOD(_NormalTex, sampler_NormalTex, v.uv,0)).rgb*0.01;
				#endif

				//todo:ddx反算normal

				float4 wPos = mul(GetObjectToWorldMatrix(), float4(v.vertex.xyz, 1.0));
				o.hPos = mul(GetWorldToHClipMatrix(), wPos);

				o.uv = v.uv;

				float3 wNormal = TransformObjectToWorldNormal(v.normal);
				float3 wTangent = TransformObjectToWorldDir(v.tangent.xyz);
				float3 wBitangent = cross(wNormal.xyz, wTangent.xyz) * v.tangent.w * GetOddNegativeScale();


				o.TBN0 = float4(wTangent.x, wBitangent.x, wNormal.x, wPos.x);
				o.TBN1 = float4(wTangent.y, wBitangent.y, wNormal.y, wPos.y);
				o.TBN2 = float4(wTangent.z, wBitangent.z, wNormal.z, wPos.z);

				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				float3 wPos = float3(i.TBN0.w, i.TBN1.w, i.TBN2.w);
				float3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, i.uv));
				float3 wNormal = float3(dot(i.TBN0.xyz, normal), dot(i.TBN1.xyz, normal), dot(i.TBN2.xyz, normal));

				float LoN = dot(_MainLightPosition.xyz, wNormal);
				half3 diffuseColor = _Color.rgb * LoN;

				diffuseColor *= SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, i.uv).rgb;

				float3 v = _WorldSpaceCameraPos - wPos;
				float3 h = normalize(_MainLightPosition.xyz + v);
				float HoN = PositivePow(dot(h, wNormal), 32);
				half3 specularColor = _MainLightColor.rgb * HoN;

				return half4(diffuseColor + specularColor, 1);
			}
			ENDHLSL
		}
	}
}