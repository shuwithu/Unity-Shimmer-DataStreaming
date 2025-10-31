Shader "Custom/ScrollingAlphaMaskedTexture"
{
    Properties
    {
        _MainTex("Texture (RGBA)", 2D) = "white" {}
        _ScrollSpeed("Scroll Speed", Float) = 1.0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        _Tiling("Tiling", Float) = 1.0
    }

        SubShader
        {
            Tags {
                "RenderType" = "TransparentCutout"
                "Queue" = "AlphaTest"
            }
            LOD 100

            Pass
            {
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

                sampler2D _MainTex;
                float4 _MainTex_ST;
                float _ScrollSpeed;
                float _Cutoff;
                float _Tiling;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex) * _Tiling;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // Vertical scrolling with tiling
                    float2 scrolledUV = i.uv;
                    scrolledUV.y += _Time.y * _ScrollSpeed;
                    scrolledUV = frac(scrolledUV); // Ensure seamless looping

                    // Sample texture and apply alpha cutoff
                    fixed4 col = tex2D(_MainTex, scrolledUV);
                    clip(col.a - _Cutoff); // Discard pixels below cutoff

                    return col;
                }
                ENDCG
            }
        }
            FallBack "Transparent/Cutout/Diffuse"
}