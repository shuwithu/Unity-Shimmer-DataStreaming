Shader "Custom/HorizontalSeamlessHyperspace"
{
    Properties
    {
        [Header(Color Settings)]
        _BaseColor("Base Color", Color) = (0.1, 0.1, 0.5, 1)
        _StreakColor("Streak Color", Color) = (0.8, 0.3, 1.0, 1)

        [Header(Animation Settings)]
        _ScrollSpeed("Scroll Speed", Float) = 1.0
        _LoopTime("Loop Duration", Float) = 5.0

        [Header(Streak Settings)]
        _StreakDensity("Streak Density", Range(0.1, 20)) = 3.0
        _StreakLength("Streak Length", Range(0.01, 1)) = 0.5
        _NoiseScale("Noise Scale", Range(0.1, 10)) = 2.0
        _StreakWidth("Streak Width", Range(0.01, 0.5)) = 0.1
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
                float _StreakWidth;

                float rand(float2 co)
                {
                    return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
                }

                float seamlessNoise(float2 uv, float scale)
                {
                    float2 uvScaled = uv * scale;
                    float2 ip = floor(uvScaled);
                    float2 fp = frac(uvScaled);
                    fp = fp * fp * (3.0 - 2.0 * fp);

                    float a = rand(ip);
                    float b = rand(ip + float2(1, 0));
                    float c = rand(ip + float2(0, 1));
                    float d = rand(ip + float2(1, 1));

                    float ab = lerp(a, b, fp.x);
                    float cd = lerp(c, d, fp.x);
                    return lerp(ab, cd, fp.y);
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
                    // Looping time calculation
                    float loopTime = fmod(_Time.y * _ScrollSpeed, _LoopTime);
                    float normalizedTime = loopTime / _LoopTime;

                    // Scrolling UVs horizontally with seamless wrapping
                    float2 uv = i.uv;
                    uv.y += loopTime; // Changed from uv.y to uv.x
                    uv = frac(uv);

                    // Create horizontal streaks
                    float streakValue = 0;
                    float streakPos = uv.x * _StreakDensity; // Changed from uv.y to uv.x

                    // Multi-layer noise for complexity
                    for (int j = 1; j <= 3; j++)
                    {
                        float layerFactor = j * 0.3;
                        float2 noiseUV = uv * _NoiseScale * layerFactor;

                        // Swapped noiseUV.x and streakPos in noise sampling
                        float n = seamlessNoise(float2(
                            streakPos * 2.0, // Now using streakPos for X
                            noiseUV.y * 5.0  // Using Y for secondary dimension
                        ), 1.0);

                        float timePhase = normalizedTime * 2.0 * UNITY_PI;
                        float streak = sin(streakPos * 10.0 + n * 10.0 + timePhase);
                        streak = smoothstep(1.0 - _StreakWidth, 1.0, abs(streak));
                        streak *= pow(n, _StreakLength * 2.0);

                        streakValue += streak * layerFactor;
                    }

                    // Combine colors (unchanged)
                    fixed4 col = _BaseColor;
                    col += _StreakColor * saturate(streakValue * 2.0);

                    // Vignette effect (unchanged)
                    float2 center = uv - 0.5;
                    float vignette = 1.0 - dot(center, center) * 2.0;
                    col *= vignette;

                    return col;
                }
                ENDCG
            }
        }
}