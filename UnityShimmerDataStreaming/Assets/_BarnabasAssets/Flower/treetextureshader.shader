Shader "Custom/LoopingHyperspace"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.1, 0.1, 0.5, 1)
        _StreakColor("Streak Color", Color) = (0.8, 0.3, 1.0, 1)
        _ScrollSpeed("Scroll Speed", Float) = 1.0
        _StreakDensity("Streak Density", Range(0.1, 10)) = 3.0
        _StreakLength("Streak Length", Range(0.01, 1)) = 0.5
        _NoiseScale("Noise Scale", Range(0.1, 10)) = 2.0
        _LoopTime("Loop Duration", Float) = 5.0
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

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

            fixed4 _BaseColor;
            fixed4 _StreakColor;
            float _ScrollSpeed;
            float _StreakDensity;
            float _StreakLength;
            float _NoiseScale;
            float _LoopTime;

            // Declare the rand function properly
            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Improved tileable noise function using the declared rand()
            float tileableNoise(float2 p, float2 period)
            {
                float2 ip = floor(p);
                float2 fp = frac(p);
                fp = fp * fp * (3.0 - 2.0 * fp);

                float a = rand(ip);
                float b = rand(ip + float2(1.0, 0.0));
                float c = rand(ip + float2(0.0, 1.0));
                float d = rand(ip + float2(1.0, 1.0));

                float e = lerp(a, b, fp.x);
                float f = lerp(c, d, fp.x);

                return lerp(e, f, fp.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Create looping time value
                float loopTime = fmod(_Time.y * _ScrollSpeed, _LoopTime);
                float normalizedTime = loopTime / _LoopTime;

                // Scroll UVs vertically with looping
                float2 uv = i.uv;
                uv.y += loopTime;

                // Tile the UV space for seamless looping
                uv = frac(uv);

                // Create streaks using tileable noise
                float streakValue = 0.0;
                float streakPos = uv.y * _StreakDensity;

                // Create multiple layers of streaks
                for (int j = 1; j <= 3; j++)
                {
                    float layerFactor = j * 0.3;
                    float2 noiseUV = uv * _NoiseScale * layerFactor;

                    // Use tileable noise for seamless looping
                    float n = tileableNoise(float2(frac(noiseUV.x * 5.0),
                                           frac(streakPos * 2.0)),
                                           float2(5.0, 2.0));

                    // Create streaks that fade in and out smoothly
                    float timeOffset = normalizedTime * 2.0 * UNITY_PI;
                    float streak = sin(streakPos * 10.0 + n * 10.0 + timeOffset);
                    streak = pow(saturate(streak), _StreakLength * 100.0);

                    // Apply smoothstep to make streaks sharper
                    streak *= smoothstep(0.3, 0.8, n);

                    // Add layer to final value
                    streakValue += streak * layerFactor;
                }

                // Combine base color with streaks
                fixed4 col = _BaseColor;
                col += _StreakColor * streakValue * 2.0;

                // Add radial vignette that also loops
                float2 center = uv - 0.5;
                float vignette = 1.0 - dot(center, center);
                col *= vignette * 1.5;

                // Add pulsing effect to enhance loop feeling
                col *= 0.9 + 0.1 * sin(normalizedTime * 2.0 * UNITY_PI);

                return col;
            }
            ENDCG
        }
    }
}