Shader "Iris/Filters/Invert"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Intensity ("Invert Intensity", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _Intensity;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // Calculate inverted color
                fixed3 invertedCol = 1.0 - col.rgb;
                // Lerp between original and inverted based on intensity
                col.rgb = lerp(col.rgb, invertedCol, _Intensity);
                return col;
            }
            ENDCG
        }
    }
}
