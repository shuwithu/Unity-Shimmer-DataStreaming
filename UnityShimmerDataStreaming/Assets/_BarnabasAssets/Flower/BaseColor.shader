Shader "Custom/ColorLerpTransparent"
{
    Properties
    {
        _ColorA ("Color A", Color) = (1, 0, 0, 0.5)
        _ColorB ("Color B", Color) = (0, 0, 1, 0.5)
        _LerpFactor ("Lerp Factor", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
                float4 pos : SV_POSITION;
            };

            fixed4 _ColorA;
            fixed4 _ColorB;
            float _LerpFactor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return lerp(_ColorA, _ColorB, _LerpFactor);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
