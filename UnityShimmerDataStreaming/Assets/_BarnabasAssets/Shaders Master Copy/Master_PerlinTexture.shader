Shader "Custom/PerlinNoiseAdvanced"
{
    Properties
    {
        _Scale("Noise Scale", Float) = 5.0
        _GradientScale("Gradient Scale", Float) = 1.0
        _Chunking("Chunking Factor", Float) = 1.0
        _Color1("Gradient Color 1", Color) = (0, 0, 1, 1)   // Blue
        _Color2("Gradient Color 2", Color) = (1, 0, 0, 1)   // Red
        _Color3("Gradient Color 3", Color) = (0, 1, 0, 1)   // Green
        _Color4("Gradient Color 4", Color) = (1, 1, 0, 1)   // Yellow
        _Color5("Gradient Color 5", Color) = (1, 0, 1, 1)   // Magenta
        _Color6("Gradient Color 6", Color) = (0, 1, 1, 1)   // Cyan
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Add these two lines for VR support:
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON

            #include "UnityCG.cginc"
            // Add this for VR support:
            #include "UnityInstancing.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                // Add this for VR support:
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                // Add this for VR support:
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Scale;
            float _GradientScale;
            float _Chunking;
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float4 _Color4;
            float4 _Color5;
            float4 _Color6;

            // [All your existing noise functions remain exactly the same...]
            float fade(float t) {
                return t * t * t * (t * (t * 6 - 15) + 10);
            }

            float2 randomGradient(int x, int y) {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h = h ^ (h >> 16);

                float angle = (h & 255) * 6.2831853 / 256.0;
                return float2(cos(angle), sin(angle));
            }

            float perlin(float2 p) {
                int x0 = floor(p.x);
                int y0 = floor(p.y);
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                float2 g00 = randomGradient(x0, y0);
                float2 g10 = randomGradient(x1, y0);
                float2 g01 = randomGradient(x0, y1);
                float2 g11 = randomGradient(x1, y1);

                float2 d00 = p - float2(x0, y0);
                float2 d10 = p - float2(x1, y0);
                float2 d01 = p - float2(x0, y1);
                float2 d11 = p - float2(x1, y1);

                float dot00 = dot(g00, d00);
                float dot10 = dot(g10, d10);
                float dot01 = dot(g01, d01);
                float dot11 = dot(g11, d11);

                float u = fade(d00.x);
                float v = fade(d00.y);

                float lerpX1 = lerp(dot00, dot10, u);
                float lerpX2 = lerp(dot01, dot11, u);
                return lerp(lerpX1, lerpX2, v) * 0.5 + 0.5;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                // Add these three lines for VR support:
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _Scale;
                return o;
            }

            // Fragment shader remains COMPLETELY UNCHANGED
            fixed4 frag(v2f i) : SV_Target
            {
                float noiseValue = perlin(i.uv);

            // Apply chunking effect
            float chunkedNoise = floor(noiseValue * _Chunking) / _Chunking;

            // Scale noise value for gradient effect
            float mappedValue = saturate(noiseValue * _GradientScale);

            // Map noise into three gradient sections
            fixed4 finalColor;
            if (mappedValue < 0.33)
            {
                float t = mappedValue / 0.33;
                finalColor = lerp(_Color1, _Color2, t);
            }
            else if (mappedValue < 0.66)
            {
                float t = (mappedValue - 0.33) / 0.33;
                finalColor = lerp(_Color3, _Color4, t);
            }
            else
            {
                float t = (mappedValue - 0.66) / 0.34;
                finalColor = lerp(_Color5, _Color6, t);
            }

            return finalColor;
        }
        ENDCG
    }
    }
}