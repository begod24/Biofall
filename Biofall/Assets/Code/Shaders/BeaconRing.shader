Shader "Biofall/BeaconRing"
{
    // Flat ground ring for the Mission 1 beacon: a glowing red disc with a bright
    // outward-pulsing alert ring, drawn procedurally from centred UVs. Additive, unlit.
    // Meant for a flat quad (rotated to lie on the ground) sized to the defense zone.
    Properties
    {
        _ColorInner ("Inner Glow", Color) = (0.8, 0.05, 0.04, 1)
        _ColorRing  ("Ring Color", Color) = (1.0, 0.25, 0.15, 1)
        _Emission   ("Emission Boost", Float) = 2.2
        _RingSpeed  ("Ring Speed", Float) = 0.6
        _RingWidth  ("Ring Width", Range(0.01, 0.4)) = 0.12
        _EdgeSoft   ("Edge Softness", Range(0.01, 0.5)) = 0.15
        _PulseSpeed ("Pulse Speed", Float) = 2.5
        _PulseAmount("Pulse Amount", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "BeaconRing"
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            float4 _ColorInner, _ColorRing;
            float _Emission, _RingSpeed, _RingWidth, _EdgeSoft, _PulseSpeed, _PulseAmount;

            Varyings vert (Attributes IN)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.uv = IN.uv;
                return o;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // centred radius 0..1 across the quad
                float2 c = IN.uv * 2.0 - 1.0;
                float r = length(c);
                if (r > 1.0) discard;

                float pulse = 1.0 - _PulseAmount + _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed));

                // soft inner fill that fades to the edge
                float inner = saturate(1.0 - r) * 0.6;

                // an outward-travelling bright ring
                float ringPos = frac(_Time.y * _RingSpeed);
                float ring = smoothstep(_RingWidth, 0.0, abs(r - ringPos));

                // a fixed rim at the boundary so the zone edge is always visible
                float rim = smoothstep(_EdgeSoft, 0.0, abs(r - 0.95));

                float3 col = _ColorInner.rgb * inner + _ColorRing.rgb * (ring + rim);
                float a = (inner + ring + rim) * pulse;

                col *= _Emission;
                return half4(col, saturate(a));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
