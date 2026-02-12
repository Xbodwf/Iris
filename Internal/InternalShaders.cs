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

        public const string BackgroundShader = @"
Shader ""Hidden/Iris/Background"" {
    Properties {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _Brightness (""Brightness"", Float) = 1.0
        _Contrast (""Contrast"", Float) = 1.0
        _Saturation (""Saturation"", Float) = 1.0
        _Hue (""Hue"", Float) = 0.0
        _Opacity (""Opacity"", Float) = 1.0
    }
    SubShader {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" }
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float _Brightness;
            float _Contrast;
            float _Saturation;
            float _Hue;
            float _Opacity;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float3 ApplyHue(float3 col, float hue) {
                float angle = hue * 3.14159265 / 180.0;
                float3 k = float3(0.57735, 0.57735, 0.57735);
                float cosAngle = cos(angle);
                return col * cosAngle + cross(k, col) * sin(angle) + k * dot(k, col) * (1.0 - cosAngle);
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 rgb = col.rgb;

                // Saturation
                float lum = dot(rgb, float3(0.299, 0.587, 0.114));
                rgb = lerp(float3(lum, lum, lum), rgb, _Saturation);

                // Contrast
                rgb = (rgb - 0.5) * _Contrast + 0.5;

                // Brightness
                rgb *= _Brightness;

                // Hue
                if (abs(_Hue) > 0.001) {
                    rgb = ApplyHue(rgb, _Hue);
                }

                return fixed4(rgb, col.a * _Opacity * i.color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}";
    }
}
