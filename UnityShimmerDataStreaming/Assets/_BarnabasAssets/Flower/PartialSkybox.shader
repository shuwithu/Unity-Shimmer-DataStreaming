Shader "Custom/PartialSkybox"
{
    Properties
    {
        _SkyboxTex("Skybox Cubemap", Cube) = "white" {}
        _FadeHeight("Fade Height", Range(0, 1)) = 0.5
        _FadeSharpness("Fade Sharpness", Range(0.1, 10)) = 1.0
        _MRBlend("MR to VR Blend", Range(0, 1)) = 0.5
    }

        SubShader
        {
            Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
            Cull Off ZWrite Off

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                };

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float3 vertex : TEXCOORD0;
                    float3 worldNormal : TEXCOORD1;
                };

                samplerCUBE _SkyboxTex;
                float _FadeHeight;
                float _FadeSharpness;
                float _MRBlend;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.vertex = v.vertex;
                    o.worldNormal = UnityObjectToWorldNormal(v.normal);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // Calculate how much we're looking up (0 = horizon, 1 = straight up)
                    float lookUpAmount = saturate(dot(float3(0,1,0), i.worldNormal));

                // Calculate fade based on look direction
                float fade = saturate((lookUpAmount - _FadeHeight) * _FadeSharpness);

                // Sample skybox
                fixed4 skyColor = texCUBE(_SkyboxTex, i.vertex);

                // Blend between MR (transparent) and VR (opaque) based on parameter
                skyColor.a = lerp(fade, 1.0, _MRBlend);

                return skyColor;
            }
            ENDCG
        }
        }
            Fallback Off
}