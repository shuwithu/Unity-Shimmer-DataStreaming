Shader "Custom/SDFCircles"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Scale("Scale of Noise", Float) = 10.0
        _Gradient("Gradient Softness", Float) = 0.2
        _Randomness("Random Variation", Float) = 0.5
        _Color1("Color 1", Color) = (1,0,0,1)
        _Color2("Color 2", Color) = (0,1,0,1)
        _Color3("Color 3", Color) = (0,0,1,1)
        _EmissiveStrength("Emissive Strength", Float) = 1.0
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
                float _Scale;
                float _Gradient;
                float _Randomness;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float _EmissiveStrength;

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
                    float2 uv = i.uv * _Scale;
                    float2 gridPos = floor(uv);
                    float2 f = frac(uv);

                    float minDist = 1.0;
                    float randIndex = random(gridPos);
                    float4 circleColor = (randIndex < 0.33) ? _Color1 : (randIndex < 0.66) ? _Color2 : _Color3;

                    for (int x = -1; x <= 1; x++) {
                        for (int y = -1; y <= 1; y++) {
                            float2 cell = gridPos + float2(x, y);
                            float randVal = random(cell) * _Randomness + (1.0 - _Randomness);
                            float2 jitter = float2(random(cell + 1.23), random(cell + 2.98)) * randVal;
                            float2 circleCenter = cell + jitter;
                            float dist = length(f - (circleCenter - gridPos));
                            minDist = min(minDist, dist);
                        }
                    }

                    float circle = smoothstep(0.5, 0.5 - _Gradient, minDist);
                    float4 finalColor = circleColor * _EmissiveStrength;
                    return fixed4(finalColor.rgb, circle);
                }
                ENDCG
            }
        }
}