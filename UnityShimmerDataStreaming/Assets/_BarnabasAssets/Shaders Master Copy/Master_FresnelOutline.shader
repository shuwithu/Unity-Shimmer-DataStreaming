Shader "Custom/PureFresnelCutout"
{
    Properties
    {
        _MainColor("Main Color", Color) = (1,1,1,1)
        _FresnelColor("Fresnel Color", Color) = (1,1,1,1)
        _FresnelPower("Fresnel Power", Range(0.1, 10)) = 5
        _FresnelBias("Fresnel Bias", Range(0, 1)) = 0.1
        _CutoutThreshold("Cutout Threshold", Range(0, 1)) = 0.5
        _AlphaMultiplier("Alpha Multiplier", Range(0, 1)) = 1
    }

        SubShader
    {
        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON

            #include "UnityCG.cginc"
            #include "UnityInstancing.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _MainColor;
            fixed4 _FresnelColor;
            float _FresnelPower;
            float _FresnelBias;
            float _CutoutThreshold;
            float _AlphaMultiplier;

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate Fresnel effect
                float fresnel = saturate(_FresnelBias + (1 - _FresnelBias) * pow(1 - dot(normalize(i.normal), normalize(i.viewDir)), _FresnelPower));

            // Combine colors
            fixed4 col = lerp(_MainColor, _FresnelColor, fresnel);

            // Apply cutout and alpha
            col.a = smoothstep(_CutoutThreshold, _CutoutThreshold + 0.1, fresnel) * _AlphaMultiplier;

            UNITY_APPLY_FOG(i.fogCoord, col);
            return col;
        }
        ENDCG
    }
    }
        FallBack "Transparent"
}