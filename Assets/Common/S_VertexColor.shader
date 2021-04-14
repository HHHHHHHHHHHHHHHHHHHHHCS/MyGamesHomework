Shader "My/S_VertexColor"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 col0 : COLOR0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 col0 : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.col0 = v.col0;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return i.col0;
            }
            ENDHLSL
        }
    }
}
