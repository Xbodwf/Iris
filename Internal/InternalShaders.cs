using UnityEngine;

namespace Iris.Internal
{
    // 使用最基础的 CGPROGRAM 语法，以确保在运行时有最高的兼容性
    public static class InternalShaders
    {
        public const string PosterizeShader = @"
Shader ""Hidden/Iris/Posterize"" {
    Properties {
        _MainTex (""Base (RGB)"", 2D) = ""white"" {}
        _Distortion (""Distortion"", Float) = 64.0
        _Fade (""Fade"", Float) = 1.0
    }
    SubShader {
        Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Distortion;
            float _Fade;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                if (_Distortion <= 0) return col;
                float div = 256.0 / _Distortion;
                fixed4 c = floor(col * div) / div;
                return lerp(col, c, _Fade);
            }
            ENDCG
        }
    }
    Fallback Off
}";

        public const string VideoBloomShader = @"
Shader ""Hidden/Iris/VideoBloom"" {
    Properties {
        _MainTex (""Base (RGB)"", 2D) = ""white"" {}
        _BloomAmount (""Bloom Amount"", Float) = 1.0
        _Threshold (""Threshold"", Float) = 0.5
        _BlurOffset (""Blur Offset"", Vector) = (1.5, 1.5, 3.5, 3.5)
    }
    SubShader {
        Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 uv_offsets : TEXCOORD1; // xy: offset1, zw: offset2
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BloomAmount;
            float _Threshold;
            float4 _BlurOffset;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv_offsets.xy = _MainTex_TexelSize.xy * _BlurOffset.xy;
                o.uv_offsets.zw = _MainTex_TexelSize.xy * _BlurOffset.zw;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // 5-tap 采样
                fixed4 s1 = tex2D(_MainTex, i.uv + i.uv_offsets.xy);
                fixed4 s2 = tex2D(_MainTex, i.uv - i.uv_offsets.xy);
                fixed4 s3 = tex2D(_MainTex, i.uv + i.uv_offsets.zw);
                fixed4 s4 = tex2D(_MainTex, i.uv - i.uv_offsets.zw);
                
                fixed4 blurred = col * 0.182 + (s1 + s2 + s3 + s4) * 0.2045;
                
                float lum = dot(blurred.rgb, float3(0.3, 0.59, 0.11));
                float weight = max(0.0, lum - _Threshold);
                
                return col + blurred * weight * _BloomAmount;
            }
            ENDCG
        }
    }
    Fallback Off
}";
    }
}
