Shader "Custom/CurvedGradient"
{
    Properties
    {
        // Color Properties
        [HDR] _ColorA("Color A", Color) = (1, 0, 0, 1)
        [HDR] _ColorB("Color B", Color) = (0, 0, 1, 1)
        [HDR] _EmissionA("Emission A", Color) = (0, 0, 0, 1)
        [HDR] _EmissionB("Emission B", Color) = (0, 0, 0, 1)
        _EmissionPower("Emission Power", Range(0, 10)) = 1

        // Gradient Controls
        _GradientPosition("Gradient Position", Range(0, 1)) = 0.5
        _GradientAngle("Gradient Angle", Range(0, 360)) = 0
        _BlendWidth("Blend Width", Range(0.01, 1)) = 0.2
        _BlendSharpness("Blend Sharpness", Range(0.1, 10)) = 1
        _Curvature("Curvature Amount", Range(-1, 1)) = 0
        _CurvatureCenter("Curvature Center", Vector) = (0.5, 0.5, 0, 0)

        // Advanced
        [Toggle] _WorldSpace("Use World Space", Float) = 0
        _ScrollSpeed("Scroll Speed", Float) = 0
        _Alpha("Overall Alpha", Range(0, 1)) = 1
    }

        SubShader
    {
        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 400

        CGPROGRAM
        #pragma surface surf Standard alpha:fade keepalpha
        #pragma target 4.0
        #include "UnityCG.cginc"

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

    // Color Properties
    float4 _ColorA;
    float4 _ColorB;
    float4 _EmissionA;
    float4 _EmissionB;
    float _EmissionPower;

    // Gradient Controls
    float _GradientPosition;
    float _GradientAngle;
    float _BlendWidth;
    float _BlendSharpness;
    float _Curvature;
    float2 _CurvatureCenter;

    // Advanced
    float _WorldSpace;
    float _ScrollSpeed;
    float _Alpha;

    // Rotate UV function
    float2 rotateUV(float2 uv, float rotation)
    {
        float rad = radians(rotation);
        float s = sin(rad);
        float c = cos(rad);
        float2x2 mat = float2x2(c, -s, s, c);
        return mul(mat, uv - 0.5) + 0.5;
    }

    // Apply curvature to gradient position
    float applyCurvature(float2 uv, float baseGradientPos)
    {
        // Calculate distance from curvature center
        float2 dir = uv - _CurvatureCenter;
        float dist = length(dir);

        // Apply curvature effect
        float curvedPos = baseGradientPos + (_Curvature * dist * dist);

        return saturate(curvedPos);
    }

    void surf(Input IN, inout SurfaceOutputStandard o)
    {
        // Calculate base coordinates
        float2 baseUV = IN.uv_MainTex;

        // Handle world space option
        if (_WorldSpace > 0.5)
        {
            float3 worldPos = IN.worldPos;
            if (abs(IN.worldNormal.y) > 0.5)
                baseUV = worldPos.xz;
            else if (abs(IN.worldNormal.x) > 0.5)
                baseUV = worldPos.zy;
            else
                baseUV = worldPos.xy;
        }

        // Rotate UV based on gradient angle
        float2 rotatedUV = rotateUV(baseUV, _GradientAngle);

        // Calculate base gradient position (with scrolling)
        float gradientPos = rotatedUV.x + _Time.y * _ScrollSpeed;
        gradientPos = frac(gradientPos);

        // Apply curvature effect
        gradientPos = applyCurvature(rotatedUV, gradientPos);

        // Calculate distance from gradient line
        float linePos = _GradientPosition;
        float dist = abs(gradientPos - linePos);

        // Adjust for blend width (with sharpness control)
        float blend = saturate((_BlendWidth - dist) / _BlendWidth);
        blend = pow(blend, _BlendSharpness);

        // Calculate final mix between colors
        float4 color = lerp(_ColorA, _ColorB, blend);
        float4 emission = lerp(_EmissionA, _EmissionB, blend) * _EmissionPower;

        // Apply to surface
        o.Albedo = color.rgb;
        o.Emission = emission.rgb;
        o.Alpha = color.a * _Alpha;
        o.Metallic = 0;
        o.Smoothness = 0.1;
    }
    ENDCG
    }
        FallBack "Transparent/Diffuse"
}