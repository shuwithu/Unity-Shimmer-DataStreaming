Shader "Unlit/CompiledTexture"
{
    Properties
    {
        _MainTex0("Master Mask", 2D) = "white" {}
        _MainTex1("Texture 1", 2D) = "white" {}
        _MainTex2("Texture 2", 2D) = "white" {}
        _MainTex3("Texture 3", 2D) = "white" {}
        _Mask1Pos("Mask 1 Position", Range(0,1)) = 0.33
        _Mask2Pos("Mask 2 Position", Range(0,1)) = 0.66
        _BlendStrength("Blend Strength", Range(0.001, 0.1)) = 0.02
        _MaskThreshold("Mask Threshold", Range(0.0, 0.5)) = 0.1
    }
        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
            Pass
            {
                ZWrite Off
                Blend SrcAlpha OneMinusSrcAlpha
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

                sampler2D _MainTex0;
                sampler2D _MainTex1;
                sampler2D _MainTex2;
                sampler2D _MainTex3;
                float4 _MainTex0_ST;
                float4 _MainTex1_ST;
                float4 _MainTex2_ST;
                float4 _MainTex3_ST;
                float _Mask1Pos;
                float _Mask2Pos;
                float _BlendStrength;
                float _MaskThreshold;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                // Helper function for smooth blending between textures
                float smoothMask(float position, float edge, float blendStrength) {
                    return smoothstep(edge - blendStrength, edge + blendStrength, position);
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // Sample master mask
                    fixed4 masterMask = tex2D(_MainTex0, TRANSFORM_TEX(i.uv, _MainTex0));

                // Calculate mask luminance (brightness)
                float maskLuminance = dot(masterMask.rgb, float3(0.299, 0.587, 0.114));

                // Early discard if mask is below threshold (almost black)
                clip(maskLuminance - _MaskThreshold);

                // Sample all textures with their respective UVs
                fixed4 tex1 = tex2D(_MainTex1, TRANSFORM_TEX(i.uv, _MainTex1));
                fixed4 tex2 = tex2D(_MainTex2, TRANSFORM_TEX(i.uv, _MainTex2));
                fixed4 tex3 = tex2D(_MainTex3, TRANSFORM_TEX(i.uv, _MainTex3));

                // Create smooth masks based on horizontal position
                float mask1 = smoothMask(i.uv.x, _Mask1Pos, _BlendStrength);
                float mask2 = smoothMask(i.uv.x, _Mask2Pos, _BlendStrength);

                // Calculate blend weights for each texture
                // Texture 1: from start to Mask1
                // Texture 2: from Mask1 to Mask2
                // Texture 3: from Mask2 to end
                float weight1 = 1.0 - mask1;
                float weight2 = mask1 * (1.0 - mask2);
                float weight3 = mask2;

                // Combine all textures with proper layering order (tex3 on top, then tex2, then tex1)
                fixed4 finalColor = tex1 * weight1;
                finalColor = lerp(finalColor, tex2, weight2 * tex2.a);
                finalColor = lerp(finalColor, tex3, weight3 * tex3.a);

                // Ensure proper alpha handling
                float finalAlpha = max(max(weight1 * tex1.a, weight2 * tex2.a), weight3 * tex3.a);
                finalColor.a = finalAlpha;

                // Apply master mask with intensity-based alpha
                finalColor.rgb *= masterMask.rgb;
                finalColor.a *= maskLuminance;

                return finalColor;
            }
            ENDCG
        }
        }
}