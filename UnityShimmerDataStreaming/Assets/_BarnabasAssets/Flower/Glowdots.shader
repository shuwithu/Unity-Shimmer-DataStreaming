Shader "Custom/SDFCircles1"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _MasterRange("Master Range", Range(0, 1)) = 0.5

            // Original parameters with their ranges
            _Scale("Scale of Noise", Float) = 10.0
            _Gradient("Gradient Softness", Float) = 0.2
            _Randomness("Random Variation", Float) = 0.5
            _Color1("Color 1", Color) = (1,0,0,1)
            _Color2("Color 2", Color) = (0,1,0,1)
            _Color3("Color 3", Color) = (0,0,1,1)
            _EmissiveStrength("Emissive Strength", Float) = 1.0

            // Hidden range parameters
            [HideInInspector] _ScaleMin("Scale Min", Float) = 5.0
            [HideInInspector] _ScaleMax("Scale Max", Float) = 20.0
            [HideInInspector] _GradientMin("Gradient Min", Float) = 0.05
            [HideInInspector] _GradientMax("Gradient Max", Float) = 0.5
            [HideInInspector] _RandomnessMin("Randomness Min", Float) = 0.1
            [HideInInspector] _RandomnessMax("Randomness Max", Float) = 0.9
            [HideInInspector] _EmissiveMin("Emissive Min", Float) = 0.5
            [HideInInspector] _EmissiveMax("Emissive Max", Float) = 3.0
    }

        SubShader
        {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                float _MasterRange;
                float _Scale;
                float _Gradient;
                float _Randomness;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float _EmissiveStrength;

                // Hidden range values
                float _ScaleMin;
                float _ScaleMax;
                float _GradientMin;
                float _GradientMax;
                float _RandomnessMin;
                float _RandomnessMax;
                float _EmissiveMin;
                float _EmissiveMax;

                float random(float2 uv) {
                    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
                }

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // Apply MasterRange to all parameters
                    float effectiveScale = lerp(_ScaleMin, _ScaleMax, _MasterRange) * (1.0 - _MasterRange) + _Scale * _MasterRange;
                    float effectiveGradient = lerp(_GradientMin, _GradientMax, _MasterRange) * (1.0 - _MasterRange) + _Gradient * _MasterRange;
                    float effectiveRandomness = lerp(_RandomnessMin, _RandomnessMax, _MasterRange) * (1.0 - _MasterRange) + _Randomness * _MasterRange;
                    float effectiveEmissive = lerp(_EmissiveMin, _EmissiveMax, _MasterRange) * (1.0 - _MasterRange) + _EmissiveStrength * _MasterRange;

                    float2 uv = i.uv * effectiveScale;
                    float2 gridPos = floor(uv);
                    float2 f = frac(uv);

                    float minDist = 1.0;
                    float randIndex = random(gridPos);
                    float4 circleColor = (randIndex < 0.33) ? _Color1 : (randIndex < 0.66) ? _Color2 : _Color3;

                    for (int x = -1; x <= 1; x++) {
                        for (int y = -1; y <= 1; y++) {
                            float2 cell = gridPos + float2(x, y);
                            float randVal = random(cell) * effectiveRandomness + (1.0 - effectiveRandomness);
                            float2 jitter = float2(random(cell + 1.23), random(cell + 2.98)) * randVal;
                            float2 circleCenter = cell + jitter;
                            float dist = length(f - (circleCenter - gridPos));
                            minDist = min(minDist, dist);
                        }
                    }

                    float circle = smoothstep(0.5, 0.5 - effectiveGradient, minDist);
                    float4 finalColor = circleColor * effectiveEmissive;
                    return fixed4(finalColor.rgb, circle);
                }
                ENDCG
            }
        }
}