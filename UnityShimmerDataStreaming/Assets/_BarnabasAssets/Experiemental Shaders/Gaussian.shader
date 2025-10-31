Shader "Custom/TransparentBlur"
{
    Properties
    {
        _BlurSize("Blur Size", Range(0, 10)) = 1.0
        _Opacity("Opacity", Range(0, 1)) = 0.8
    }

        SubShader
    {
        Tags { "Queue" = "Transparent" }

        GrabPass { "_GrabTexture" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 grabPos : TEXCOORD0;
            };

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;
            float _BlurSize;
            float _Opacity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Gaussian weights
                static const half weights[5] = {0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216};

                half2 uv = i.grabPos.xy / i.grabPos.w;
                half4 color = tex2D(_GrabTexture, uv) * weights[0];

                // Horizontal blur
                for (int j = 1; j < 5; j++)
                {
                    half2 offset = half2(_GrabTexture_TexelSize.x * j * _BlurSize, 0);
                    color += tex2D(_GrabTexture, uv + offset) * weights[j];
                    color += tex2D(_GrabTexture, uv - offset) * weights[j];
                }

                // Vertical blur
                half4 finalColor = color * weights[0];
                for (int j = 1; j < 5; j++)
                {
                    half2 offset = half2(0, _GrabTexture_TexelSize.y * j * _BlurSize);
                    finalColor += tex2D(_GrabTexture, uv + offset) * weights[j];
                    finalColor += tex2D(_GrabTexture, uv - offset) * weights[j];
                }

                return finalColor * _Opacity;
            }
            ENDCG
        }
    }
}