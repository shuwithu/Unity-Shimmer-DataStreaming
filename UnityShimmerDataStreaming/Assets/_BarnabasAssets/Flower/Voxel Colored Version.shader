Shader "Custom/PerlinVoxelizationShader" {
    Properties{
        _MainTex("Texture", 2D) = "white" {}
        _VoxelSize("Voxel Size", Range(0.01, 1)) = 0.1
        _VolumeCenter("Volume Center", Vector) = (0,0,0,0)
        _VolumeExtents("Volume Extents", Vector) = (5,5,5,0)

            // Gradient Color Parameters
            _ColorStart("Gradient Start Color", Color) = (1,0,0,1)
            _ColorMiddle("Gradient Middle Color", Color) = (0,1,0,1)
            _ColorEnd("Gradient End Color", Color) = (0,0,1,1)
            _GradientBlend("Gradient Blend Factor", Range(0, 1)) = 0.5

            // Perlin Noise Parameters
            _NoiseScale("Noise Scale", Range(0.1, 10)) = 1.0
            _NoiseInfluence("Noise Influence", Range(0, 1)) = 0.5
            _TransitionSharpness("Transition Sharpness", Range(0.1, 10)) = 2.0
    }
        SubShader{
            Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
            LOD 100
            CGPROGRAM
            #pragma surface surf Standard fullforwardshadows
            #pragma target 3.0

            sampler2D _MainTex;
            float _VoxelSize;
            float3 _VolumeCenter;
            float3 _VolumeExtents;
            float4 _ColorStart;
            float4 _ColorMiddle;
            float4 _ColorEnd;
            float _GradientBlend;
            float _NoiseScale;
            float _NoiseInfluence;
            float _TransitionSharpness;

            struct Input {
                float2 uv_MainTex;
                float3 worldPos;
                float3 worldNormal;
            };

            // Perlin Noise Implementation
            float2 hash2D(float2 p) {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
            }

            float perlinNoise(float3 p) {
                float3 pi = floor(p);
                float3 pf = frac(p);
                float3 w = pf * pf * (3.0 - 2.0 * pf);

                float n000 = dot(hash2D(pi.xy), pf.xy);
                float n100 = dot(hash2D(pi.xy + float2(1.0, 0.0)), pf.xy - float2(1.0, 0.0));
                float n010 = dot(hash2D(pi.xy + float2(0.0, 1.0)), pf.xy - float2(0.0, 1.0));
                float n110 = dot(hash2D(pi.xy + float2(1.0, 1.0)), pf.xy - float2(1.0, 1.0));

                float nx00 = lerp(n000, n100, w.x);
                float nx10 = lerp(n010, n110, w.x);
                float nxy0 = lerp(nx00, nx10, w.y);

                return nxy0 * 0.5 + 0.5; // Normalize to 0-1 range
            }

            // Check if point is inside defined volume
            bool IsInsideVolume(float3 worldPos) {
                float3 localPos = worldPos - _VolumeCenter;
                return abs(localPos.x) <= _VolumeExtents.x &&
                       abs(localPos.y) <= _VolumeExtents.y &&
                       abs(localPos.z) <= _VolumeExtents.z;
            }

            // Voxel grid quantization relative to volume center
            float3 QuantizeToVoxelGrid(float3 worldPos) {
                float3 localPos = worldPos - _VolumeCenter;
                return _VolumeCenter + floor(localPos / _VoxelSize) * _VoxelSize;
            }

            // Sample from color gradient based on t (0-1)
            float4 SampleGradient(float t) {
                if (t < 0.5) {
                    // Blend between start and middle
                    return lerp(_ColorStart, _ColorMiddle, t * 2.0);
                }
     else {
                    // Blend between middle and end
                    return lerp(_ColorMiddle, _ColorEnd, (t - 0.5) * 2.0);
                }
            }

            void surf(Input IN, inout SurfaceOutputStandard o) {
                // Sample original texture
                fixed4 texColor = tex2D(_MainTex, IN.uv_MainTex);

                // Check if current fragment is inside volume
                if (IsInsideVolume(IN.worldPos)) {
                    // Quantize world position to voxel grid
                    float3 voxelPos = QuantizeToVoxelGrid(IN.worldPos);

                    // Calculate distance from current position to voxel center
                    float3 distToCenter = (IN.worldPos - voxelPos) / _VoxelSize;
                    float normalizedDist = length(distToCenter) / sqrt(3.0); // Normalize 0-1

                    // Add perlin noise influence to create natural transitions
                    float noise = perlinNoise(IN.worldPos * _NoiseScale);
                    float distWithNoise = saturate(normalizedDist + (noise - 0.5) * _NoiseInfluence);

                    // Apply transition sharpness
                    float blendFactor = pow(1.0 - distWithNoise, _TransitionSharpness);

                    // Generate a consistent value based on voxel position for gradient sampling
                    float3 colorSeed = voxelPos;
                    float gradientPos = frac(sin(dot(colorSeed, float3(12.9898, 78.233, 45.164))) * 43758.5453);

                    // Sample from the color gradient
                    float4 gradientColor = SampleGradient(gradientPos);

                    // Blend between original texture and gradient color using noise-based transitions
                    o.Albedo = lerp(texColor.rgb, gradientColor.rgb, blendFactor * _GradientBlend);
                    o.Alpha = texColor.a;
                }
                else {
                    // Outside volume, render normally
                    o.Albedo = texColor.rgb;
                    o.Alpha = texColor.a;
                }
            }
            ENDCG
        }
            FallBack "Diffuse"
}