Shader "Custom/FlowFresnel"
{
    Properties
    {
        _MainColor("Main Color", Color) = (0.2, 0.5, 1, 0.3)
        _FresnelColor("Fresnel Color", Color) = (0.8, 0.9, 1, 1)
        _FresnelPower("Fresnel Power", Range(0.1, 10)) = 3
        _FresnelBias("Fresnel Bias", Range(0, 1)) = 0.2

        // Flow controls
        _FlowSpeed("Flow Speed", Range(0, 5)) = 1
        _FlowIntensity("Flow Intensity", Range(0, 1)) = 0.3
        _FlowScale("Flow Scale", Range(0.1, 10)) = 2
        _FlowDirection("Flow Direction", Vector) = (1, 0.5, 0, 0)

        // Distortion
        _DistortionAmount("Distortion Amount", Range(0, 0.5)) = 0.1
        _DistortionSpeed("Distortion Speed", Range(0, 2)) = 0.5
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
            float _FresnelPower;
            float _FresnelBias;

            // Flow properties
            float _FlowSpeed;
            float _FlowIntensity;
            float _FlowScale;
            float4 _FlowDirection;

            // Distortion properties
            float _DistortionAmount;
            float _DistortionSpeed;

            // Simple noise function for distortion
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

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));

                // Calculate flow UVs based on world position and time
                float3 flowDir = normalize(_FlowDirection.xyz);
                o.flowUV = (o.worldPos * _FlowScale) + (flowDir * _Time.y * _FlowSpeed);

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Create distortion pattern
                float distortion = noise(i.flowUV * 2 + _Time.y * _DistortionSpeed) * _DistortionAmount;

            // Animate the normal with flow effect
            float3 animatedNormal = i.normal;
            animatedNormal.x += sin(i.flowUV.x + _Time.y * _FlowSpeed) * _FlowIntensity;
            animatedNormal.y += cos(i.flowUV.y + _Time.y * _FlowSpeed * 0.7) * _FlowIntensity;
            animatedNormal = normalize(animatedNormal + distortion);

            // Calculate Fresnel effect with animated normal
            float fresnel = saturate(_FresnelBias + (1 - _FresnelBias) *
                            pow(1 - dot(normalize(animatedNormal), normalize(i.viewDir)), _FresnelPower));

            // Add pulsing effect to fresnel
            fresnel *= 1.0 + (sin(_Time.y * _FlowSpeed * 0.5) * 0.1);

            // Combine colors
            fixed4 col = lerp(_MainColor, _FresnelColor, fresnel);

            // Make edges more opaque than center
            col.a = fresnel * _MainColor.a;

            UNITY_APPLY_FOG(i.fogCoord, col);
            return col;
        }
        ENDCG
    }
    }
        FallBack "Transparent"
}