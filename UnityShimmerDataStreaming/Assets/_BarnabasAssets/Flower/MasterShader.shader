Shader "Custom/MasterShader"
{
    Properties
    {
        // Common properties
        [Toggle] _EnablePerlin("Enable Perlin Noise", Float) = 0
        [Toggle]_EnableVoxel("Enable Voxelization", Float) = 0
        [Toggle]_EnableSDF("Enable SDF Circles", Float) = 0
        [Toggle]_EnableTexture("Enable Texture", Float) = 0

        // Perlin Noise properties
        _Scale("Noise Scale", Float) = 5.0
        _GradientScale("Gradient Scale", Float) = 1.0
        _Chunking("Chunking Factor", Float) = 1.0
        _Color1("Gradient Color 1", Color) = (0, 0, 1, 1)
        _Color2("Gradient Color 2", Color) = (1, 0, 0, 1)
        _Color3("Gradient Color 3", Color) = (0, 1, 0, 1)
        _Color4("Gradient Color 4", Color) = (1, 1, 0, 1)
        _Color5("Gradient Color 5", Color) = (1, 0, 1, 1)
        _Color6("Gradient Color 6", Color) = (0, 1, 1, 1)

        // Voxel properties
        _MainTex("Texture", 2D) = "white" {}
        _VoxelSize("Voxel Size", Range(0.01, 1)) = 0.1
        _VolumeCenter("Volume Center", Vector) = (0,0,0,0)
        _VolumeExtents("Volume Extents", Vector) = (5,5,5,0)
        _VoxelColor("Voxel Color", Color) = (1,1,1,1)
        _VoxelBlend("Voxel Blend", Range(0, 1)) = 0.5

            // SDF Circles properties
            _SDFScale("Scale of Noise", Float) = 10.0
            _Gradient("Gradient Softness", Float) = 0.2
            _Randomness("Random Variation", Float) = 0.5
            _SDFColor1("SDF Color 1", Color) = (1,0,0,1)
            _SDFColor2("SDF Color 2", Color) = (0,1,0,1)
            _SDFColor3("SDF Color 3", Color) = (0,0,1,1)
            _EmissiveStrength("Emissive Strength", Float) = 1.0
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
                #pragma shader_feature _ENABLEPERLIN_ON
                #pragma shader_feature _ENABLEVOXEL_ON
                #pragma shader_feature _ENABLESDF_ON
                #pragma shader_feature _ENABLETEXTURE_ON

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    float3 normal : NORMAL;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    float3 worldPos : TEXCOORD1;
                    float3 worldNormal : TEXCOORD2;
                };

                // Common variables
                sampler2D _MainTex;
                float4 _MainTex_ST;

                // Perlin Noise variables
                float _Scale;
                float _GradientScale;
                float _Chunking;
                float4 _Color1, _Color2, _Color3, _Color4, _Color5, _Color6;

                // Voxel variables
                float _VoxelSize;
                float3 _VolumeCenter;
                float3 _VolumeExtents;
                float4 _VoxelColor;
                float _VoxelBlend;

                // SDF Circles variables
                float _SDFScale;
                float _Gradient;
                float _Randomness;
                float4 _SDFColor1, _SDFColor2, _SDFColor3;
                float _EmissiveStrength;

                // ===== Perlin Noise Functions =====
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

                float4 applyPerlin(float2 uv) {
                    float noiseValue = perlin(uv);
                    float chunkedNoise = floor(noiseValue * _Chunking) / _Chunking;
                    float mappedValue = saturate(noiseValue * _GradientScale);

                    if (mappedValue < 0.33) {
                        float t = mappedValue / 0.33;
                        return lerp(_Color1, _Color2, t);
                    }
                    else if (mappedValue < 0.66) {
                        float t = (mappedValue - 0.33) / 0.33;
                        return lerp(_Color3, _Color4, t);
                    }
                    else {
                        float t = (mappedValue - 0.66) / 0.34;
                        return lerp(_Color5, _Color6, t);
                    }
                }

                // ===== Voxel Functions =====
                bool IsInsideVolume(float3 worldPos) {
                    float3 localPos = worldPos - _VolumeCenter;
                    return abs(localPos.x) <= _VolumeExtents.x &&
                           abs(localPos.y) <= _VolumeExtents.y &&
                           abs(localPos.z) <= _VolumeExtents.z;
                }

                float3 QuantizeToVoxelGrid(float3 worldPos) {
                    float3 localPos = worldPos - _VolumeCenter;
                    return _VolumeCenter + floor(localPos / _VoxelSize) * _VoxelSize;
                }

                float4 applyVoxel(float2 uv, float3 worldPos, float3 worldNormal) {
                    fixed4 texColor = tex2D(_MainTex, uv);

                    if (IsInsideVolume(worldPos)) {
                        float3 voxelPos = QuantizeToVoxelGrid(worldPos);
                        float3 colorSeed = voxelPos;
                        float3 voxelColor = frac(sin(dot(colorSeed, float3(12.9898, 78.233, 45.164))) * 43758.5453);
                        return lerp(texColor, float4(voxelColor * _VoxelColor.rgb, texColor.a), _VoxelBlend);
                    }
                    return texColor;
                }

                // ===== SDF Circles Functions =====
                float sdf_random(float2 uv) {
                    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
                }

                float4 applySDF(float2 uv) {
                    uv *= _SDFScale;
                    float2 gridPos = floor(uv);
                    float2 f = frac(uv);

                    float minDist = 1.0;
                    float4 finalColor = float4(0, 0, 0, 0);

                    for (int x = -1; x <= 1; x++) {
                        for (int y = -1; y <= 1; y++) {
                            float2 cell = gridPos + float2(x, y);
                            float randVal = sdf_random(cell) * _Randomness + (1.0 - _Randomness);
                            float2 jitter = float2(sdf_random(cell + 1.23), sdf_random(cell + 2.98)) * randVal;
                            float2 circleCenter = cell + jitter;
                            float dist = length(f - (circleCenter - gridPos));

                            if (dist < minDist) {
                                minDist = dist;
                                float randIndex = sdf_random(cell);
                                finalColor = (randIndex < 0.33) ? _SDFColor1 :
                                            (randIndex < 0.66) ? _SDFColor2 : _SDFColor3;
                            }
                        }
                    }

                    float circle = smoothstep(0.5, 0.5 - _Gradient, minDist);
                    return float4(finalColor.rgb * _EmissiveStrength, circle);
                }

                // ===== Vertex/Fragment Shaders =====
                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    o.worldNormal = UnityObjectToWorldNormal(v.normal);
                    return o;
                }

                float4 frag(v2f i) : SV_Target
                {
                    float4 finalColor = float4(0, 0, 0, 1);

                    // Base texture (always applied if enabled)
                    #if _ENABLETEXTURE_ON
                        float4 texColor = tex2D(_MainTex, i.uv);
                        finalColor = float4(i.uv, 1, 0) * (texColor + 0.7);
                    #endif

                        // Apply effects in order of priority
                        #if _ENABLEPERLIN_ON
                            float4 perlinColor = applyPerlin(i.uv);
                            finalColor = lerp(finalColor, perlinColor, perlinColor.a);
                        #endif

                        #if _ENABLEVOXEL_ON
                            float4 voxelColor = applyVoxel(i.uv, i.worldPos, i.worldNormal);
                            finalColor = lerp(finalColor, voxelColor, _VoxelBlend);
                        #endif

                        #if _ENABLESDF_ON
                            float4 sdfColor = applySDF(i.uv);
                            finalColor = lerp(finalColor, sdfColor, sdfColor.a);
                        #endif

                        return finalColor;
                    }
                    ENDCG
                }
        }
            FallBack "Diffuse"
}