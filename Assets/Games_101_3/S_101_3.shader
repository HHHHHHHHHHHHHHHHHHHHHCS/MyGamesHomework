//单纯的偷懒 不用代码写了  直接用shader完成
Shader "Games/S_101_3"
{
	Properties
	{
		_Color ("Color", Color) = (0.4,0.4,0.2,1)
		_AlbedoTex ("Albedo Texture", 2D) = "white" {}
		[Toggle] _Use_Displacement("Use Displacement", int) = 0
		_DisplacementStrength("Displacement Strength", float) = 0.05
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
				#if !_USE_DISPLACEMENT_ON
				float4 TBN0 : TEXCOORD1;
				float4 TBN1 : TEXCOORD2;
				float4 TBN2 : TEXCOORD3;
				#else
				float4 wPos : TEXCOORD1;
				#endif
			};

			CBUFFER_START(UnityPerMaterial)
			half4 _Color;
			#if _USE_DISPLACEMENT_ON
			float _DisplacementStrength;
			#endif
			float4 _AlbedoTex_TexelSize;
			CBUFFER_END

			TEXTURE2D(_AlbedoTex);
			SAMPLER(sampler_AlbedoTex);
			SAMPLER(sampler_clamp_point_AlbedoTex);
			SAMPLER(sampler_clamp_linear_AlbedoTex);
			SAMPLER(sampler_clamp_bilinear_AlbedoTex);
			SAMPLER(sampler_clamp_trilinear_AlbedoTex);

			TEXTURE2D(_NormalTex);
			SAMPLER(sampler_NormalTex);


			v2f vert(a2v v)
			{
				v2f o;

				#if _USE_DISPLACEMENT_ON
					//为什么vs 不能用 SAMPLE_TEXTURE2D 而是用SAMPLE_TEXTURE2D_LOD
					//因为ddx ddy 是在fs中才有的
					//https://blog.csdn.net/u013746357/article/details/107975128
					v.vertex.xyz += v.normal.xyz * UnpackNormal(SAMPLE_TEXTURE2D_LOD(_NormalTex, sampler_NormalTex, v.uv,0)).rgb * _DisplacementStrength;
				#endif

				float4 wPos = mul(GetObjectToWorldMatrix(), float4(v.vertex.xyz, 1.0));
				o.hPos = mul(GetWorldToHClipMatrix(), wPos);

				o.uv = v.uv;

				#if !_USE_DISPLACEMENT_ON
				float3 wNormal = TransformObjectToWorldNormal(v.normal);
				float3 wTangent = TransformObjectToWorldDir(v.tangent.xyz);
				float3 wBitangent = cross(wNormal.xyz, wTangent.xyz) * v.tangent.w * GetOddNegativeScale();


				o.TBN0 = float4(wTangent.x, wBitangent.x, wNormal.x, wPos.x);
				o.TBN1 = float4(wTangent.y, wBitangent.y, wNormal.y, wPos.y);
				o.TBN2 = float4(wTangent.z, wBitangent.z, wNormal.z, wPos.z);
				#else
					o.wPos = wPos;
				#endif


				return o;
			}

			float CalcMipmapLOD(float2 uv, float2 texelSize)
			{
				float dx = ddx(uv.x);
				float dy = ddy(uv.y);

				float px = texelSize.x * dx;
				float py = texelSize.y * dy;
				float lod = log2(max(dot(px, px), dot(py, py)));

				return max(0, lod);
			}

			half4 GetColorLinear(float2 uv,TEXTURE2D(_Tex), float2 texelSize)
			{
				float2 pos = uv * texelSize;
				float2 fracP = frac(pos) - 0.5;
				float2 signP = sign(fracP);
				float2 absP = abs(fracP);
				float2 offset = float2(absP.x >= absP.y, absP.y >= absP.x);

				float2 p0 = floor(pos);
				float2 p1 = p0 + signP * offset;

				half4 c0 = LOAD_TEXTURE2D(_Tex, p0);
				half4 c1 = LOAD_TEXTURE2D(_Tex, p1);

				float t = 2 * dot(offset, absP) / dot(offset, offset);

				return lerp(c0, c1, t);
			}

			half4 GetColorBilinear(float2 uv,TEXTURE2D(_Tex), float2 texelSize)
			{
				float2 pos = uv * texelSize;
				float2 fracP = frac(pos) - 0.5;
				float2 signP = sign(fracP);
				float2 absP = abs(fracP);

				float2 p0 = floor(pos);
				float2 p1 = p0 + float2(signP.x, 0);
				float2 p2 = p0 + float2(0, signP.y);
				float2 p3 = p0 + signP.xy;

				half4 c0 = LOAD_TEXTURE2D(_Tex, p0);
				half4 c1 = LOAD_TEXTURE2D(_Tex, p1);
				half4 c2 = LOAD_TEXTURE2D(_Tex, p2);
				half4 c3 = LOAD_TEXTURE2D(_Tex, p3);

				c0 = lerp(c0, c1, absP.x * 2);
				c2 = lerp(c2, c3, absP.x * 2);
				c0 = lerp(c0, c2, absP.y * 2);

				return c0;
			}

			half4 GetColorTrilinear_LOD(float2 uv,TEXTURE2D(_Tex), float2 texelSize, int lod)
			{
				float2 pos = uv * texelSize / pow(2, lod);
				float2 fracP = frac(pos) - 0.5;
				float2 signP = sign(fracP);
				float2 absP = abs(fracP);

				float2 p0 = floor(pos);
				float2 p1 = p0 + float2(signP.x, 0);
				float2 p2 = p0 + float2(0, signP.y);
				float2 p3 = p0 + signP.xy;

				half4 c0 = LOAD_TEXTURE2D_LOD(_Tex, p0, lod);
				half4 c1 = LOAD_TEXTURE2D_LOD(_Tex, p1, lod);
				half4 c2 = LOAD_TEXTURE2D_LOD(_Tex, p2, lod);
				half4 c3 = LOAD_TEXTURE2D_LOD(_Tex, p3, lod);

				c0 = lerp(c0, c1, absP.x * 2);
				c2 = lerp(c2, c3, absP.x * 2);
				c0 = lerp(c0, c2, absP.y * 2);

				return c0;
			}

			half4 GetColorTrilinear(float2 uv,TEXTURE2D(_Tex), float2 texelSize)
			{
				float lod = CalcMipmapLOD(uv, texelSize);

				half4 c0 = GetColorTrilinear_LOD(uv, _Tex, texelSize, (int)lod);
				half4 c1 = GetColorTrilinear_LOD(uv, _Tex, texelSize, max(0, (int)lod - 1));

				return lerp(c0, c1, 1 - frac(lod));
			}


			half4 frag(v2f i) : SV_Target
			{
				// return SAMPLE_TEXTURE2D_LOD(_AlbedoTex, sampler_clamp_point_AlbedoTex, i.uv, 0);
				// return SAMPLE_TEXTURE2D_LOD(_AlbedoTex, sampler_clamp_bilinear_AlbedoTex, i.uv, 0);
				// return SAMPLE_TEXTURE2D_LOD(_AlbedoTex, sampler_clamp_trilinear_AlbedoTex, i.uv, 0);
				// return LOAD_TEXTURE2D(_AlbedoTex, i.uv*_AlbedoTex_TexelSize.zw);
				// return GetColorLinear(i.uv, _AlbedoTex, _AlbedoTex_TexelSize.zw);
				// return GetColorBilinear(i.uv, _AlbedoTex, _AlbedoTex_TexelSize.zw);
				// return GetColorTrilinear(i.uv, _AlbedoTex, _AlbedoTex_TexelSize.zw);


				#if !_USE_DISPLACEMENT_ON
				float3 wPos = float3(i.TBN0.w, i.TBN1.w, i.TBN2.w);
				float3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, i.uv));
				float3 wNormal = float3(dot(i.TBN0.xyz, normal), dot(i.TBN1.xyz, normal), dot(i.TBN2.xyz, normal));
				#else
					float3 wPos = i.wPos;
					float3 wNormal = normalize(cross(ddy(wPos),ddx(wPos)));
				#endif

				float LoN = dot(_MainLightPosition.xyz, wNormal);
				half3 diffuseColor = _Color.rgb * LoN;

				//这里只做案例 跟颜色相关   不做normal
				//SAMPLE_TEXTURE2D   SAMPLE_TEXTURE2D_LOD

				// float lod = CalcMipmapLOD(i.uv, _AlbedoTex_TexelSize.zw);
				// diffuseColor *= SAMPLE_TEXTURE2D_LOD(_AlbedoTex, sampler_AlbedoTex, i.uv, lod).rgb;

				diffuseColor *= GetColorTrilinear(i.uv, _AlbedoTex, _AlbedoTex_TexelSize.zw).rgb;
				
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