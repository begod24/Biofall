Shader "Biofall/BloodDecal"
{
    // Cheap flat ground decal with a PROCEDURAL irregular blood-pool shape (no texture fetch).
    // Unlit, alpha-blended, double-sided, no depth write, SRP-batched — hundreds cost almost nothing.
    Properties
    {
        _BaseColor ("Tint", Color) = (0.32, 0.015, 0.015, 1)
        _Wobble ("Edge Wobble", Range(0,0.25)) = 0.13
        _Softness ("Edge Softness", Range(0.01,0.3)) = 0.08
        _Seed ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Wobble;
                float _Softness;
                float _Seed;
            CBUFFER_END

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = GetVertexPositionInputs(IN.positionOS.xyz).positionCS;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv - 0.5;
                float ang = atan2(p.y, p.x);
                float r = length(p) * 2.0;                 // 1.0 at mid-edge, ~1.41 at corners

                // Irregular boundary radius (a few harmonics offset by the seed for variety).
                float s = _Seed;
                float edge = 0.74
                    + _Wobble * sin(ang * 3.0 + s * 1.7)
                    + _Wobble * 0.6 * sin(ang * 5.0 - s * 2.3 + 2.0)
                    + _Wobble * 0.4 * sin(ang * 8.0 + s + 4.0);

                float a = 1.0 - smoothstep(edge - _Softness, edge, r);

                // Mottled grit so it doesn't look like a flat sticker.
                float n = hash(floor(IN.uv * 48.0) + s);
                a *= lerp(0.82, 1.0, n);
                a *= _BaseColor.a;
                clip(a - 0.01);

                // Slightly darker, thicker-looking centre.
                float3 rgb = _BaseColor.rgb * lerp(0.7, 1.05, saturate(r / edge));
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
}
