Shader "Custom/UniversalVoxelShader" {
    Properties{
        _MainTex("Texture", 2D) = "white" {}
        _VoxelSize("Voxel Size", Range(0.0001, 0.009)) = 0.05
        _VoxelColor("Voxel Color", Color) = (1,1,1,1)
        _VoxelBlend("Voxel Blend", Range(0, 1)) = 0.5
        _MasterRange("Master Range", Range(0, 1)) = 1.0
    }

        SubShader{
            Tags { "RenderType" = "Opaque" }
            LOD 100

            CGPROGRAM
            #pragma surface surf Standard fullforwardshadows
            #pragma target 3.0

            sampler2D _MainTex;
            float _VoxelSize;
            float4 _VoxelColor;
            float _VoxelBlend;
            float _MasterRange;

            struct Input {
                float2 uv_MainTex;
                float3 worldPos;
            };

            float3 QuantizeToVoxelGrid(float3 worldPos) {
                float effectiveVoxelSize = lerp(_VoxelSize, _VoxelSize * 0.001, _MasterRange);
                return floor(worldPos / effectiveVoxelSize) * effectiveVoxelSize;
            }

            void surf(Input IN, inout SurfaceOutputStandard o) {
                fixed4 texColor = tex2D(_MainTex, IN.uv_MainTex);

                // Always apply voxelization
                float3 voxelPos = QuantizeToVoxelGrid(IN.worldPos);
                float3 colorSeed = voxelPos;
                float3 voxelColor = frac(sin(dot(colorSeed, float3(12.9898, 78.233, 45.164))) * 43758.5453);

                o.Albedo = lerp(texColor.rgb, voxelColor * _VoxelColor.rgb, _VoxelBlend);
                o.Alpha = texColor.a;
            }
            ENDCG
        }
            FallBack "Diffuse"
}