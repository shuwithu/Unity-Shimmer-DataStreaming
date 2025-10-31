Shader "Custom/FlowFresnel"
{
    Properties
    {
        _MainColor("Main Color", Color) = (0.2, 0.5, 1, 0.3)
        _FresnelColor("Fresnel Color", Color) = (0.8, 0.9, 1, 1)
        _MasterRange("Master Range", Range(0, 1)) = 0.5

        // Original parameters with their ranges preserved
        _FresnelPower("Fresnel Power", Range(0.1, 10)) = 3
        _FresnelBias("Fresnel Bias", Range(0, .6)) = 0.2
        _FlowSpeed("Flow Speed", Range(0, 5)) = 1
        _FlowIntensity("Flow Intensity", Range(0, 1)) = 0.3
        _FlowScale("Flow Scale", Range(0.1, 10)) = 2
        _DistortionAmount("Distortion Amount", Range(0, 0.5)) = 0.1
        _DistortionSpeed("Distortion Speed", Range(0, 2)) = 0.5

        _FlowDirection("Flow Direction", Vector) = (1, 0.5, 0, 0)
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

            // Property ranges
            #define FRESNEL_POWER_MIN 0.1
            #define FRESNEL_POWER_MAX 10.0
            #define FRESNEL_BIAS_MIN 0.0
            #define FRESNEL_BIAS_MAX 0.6
            #define FLOW_INTENSITY_MIN 0.0
            #define FLOW_INTENSITY_MAX 1.0
            #define DISTORTION_AMOUNT_MIN 0.0
            #define DISTORTION_AMOUNT_MAX 0.5
            #define FLOW_SPEED_MIN 0.0
            #define FLOW_SPEED_MAX 5.0
            #define FLOW_SCALE_MIN 0.1
            #define FLOW_SCALE_MAX 10.0
            #define DISTORTION_SPEED_MIN 0.0
            #define DISTORTION_SPEED_MAX 2.0

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float3 flowUV : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _MainColor;
            fixed4 _FresnelColor;
            float _MasterRange;

            // Original properties
            float _FresnelPower;
            float _FresnelBias;
            float _FlowSpeed;
            float _FlowIntensity;
            float _FlowScale;
            float4 _FlowDirection;
            float _DistortionAmount;
            float _DistortionSpeed;

            float noise(float3 p)
            {
                return frac(sin(dot(p, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Calculate flow parameters using MasterRange and property ranges
                float flowSpeed = lerp(FLOW_SPEED_MIN, FLOW_SPEED_MAX, _MasterRange);
                float flowScale = lerp(FLOW_SCALE_MIN, FLOW_SCALE_MAX, _MasterRange);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));

                float3 flowDir = normalize(_FlowDirection.xyz);
                o.flowUV = (o.worldPos * flowScale) + (flowDir * _Time.y * flowSpeed);

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate parameters using MasterRange and property ranges
                float fresnelPower = lerp(FRESNEL_POWER_MIN, FRESNEL_POWER_MAX, _MasterRange);
                float fresnelBias = lerp(FRESNEL_BIAS_MIN, FRESNEL_BIAS_MAX, _MasterRange);
                float flowIntensity = lerp(FLOW_INTENSITY_MIN, FLOW_INTENSITY_MAX, _MasterRange);
                float distortionAmount = lerp(DISTORTION_AMOUNT_MIN, DISTORTION_AMOUNT_MAX, _MasterRange);
                float distortionSpeed = lerp(DISTORTION_SPEED_MIN, DISTORTION_SPEED_MAX, _MasterRange);

                // Create distortion pattern
                float distortion = noise(i.flowUV * 2 + _Time.y * distortionSpeed) * distortionAmount;

                // Animate the normal with flow effect
                float3 animatedNormal = i.normal;
                animatedNormal.x += sin(i.flowUV.x + _Time.y * _FlowSpeed) * flowIntensity;
                animatedNormal.y += cos(i.flowUV.y + _Time.y * _FlowSpeed * 0.7) * flowIntensity;
                animatedNormal = normalize(animatedNormal + distortion);

                // Calculate Fresnel effect
                float fresnel = saturate(fresnelBias + (1 - fresnelBias) *
                                pow(1 - dot(normalize(animatedNormal), normalize(i.viewDir)), fresnelPower));

                // Add pulsing effect
                fresnel *= 1.0 + (sin(_Time.y * _FlowSpeed * 0.5) * 0.1);

                // Combine colors
                fixed4 col = lerp(_MainColor, _FresnelColor, fresnel);
                col.a = fresnel * _MainColor.a;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
        FallBack "Transparent"
}