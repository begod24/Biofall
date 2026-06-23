Shader "Biofall/AcidPool"
{
    // Toxic, corrosive acid pool decal laid flat on the ground (the territory a Spitter has spat on).
    // Procedural, unlit, alpha-blended, SRP-batched: a dark swamp-green body with animated bubbling
    // hot-spots and a bright corrosive rim hugging an irregular boundary. _Life (driven by AcidPool.cs)
    // dries the whole thing out as it expires.
    Properties
    {
        _BaseColor   ("Acid Tint", Color) = (0.10, 0.26, 0.05, 0.9)
        _HotColor    ("Hot / Bubble Color", Color) = (0.55, 1.0, 0.12, 1)
        _EdgeColor   ("Corrosive Edge", Color) = (0.78, 1.0, 0.22, 1)
        _Wobble      ("Edge Wobble", Range(0,0.25)) = 0.1
        _Softness    ("Edge Softness", Range(0.01,0.3)) = 0.07
        _Seed        ("Seed", Float) = 0
        _BubbleScale ("Bubble Scale", Range(2,40)) = 14
        _BubbleSpeed ("Bubble Speed", Range(0,4)) = 1.3
        _RimWidth    ("Rim Width", Range(0.02,0.4)) = 0.16
        _Life        ("Life (1=fresh)", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
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
                float4 _HotColor;
                float4 _EdgeColor;
                float _Wobble;
                float _Softness;
                float _Seed;
                float _BubbleScale;
                float _BubbleSpeed;
                float _RimWidth;
                float _Life;
            CBUFFER_END

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
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
                float r = length(p) * 2.0;

                // Irregular blob boundary (a few harmonics offset by the seed).
                float s = _Seed;
                float edge = 0.82
                    + _Wobble * sin(ang * 3.0 + s * 1.7)
                    + _Wobble * 0.6 * sin(ang * 5.0 - s * 2.3 + 2.0)
                    + _Wobble * 0.4 * sin(ang * 8.0 + s + 4.0);

                float body = 1.0 - smoothstep(edge - _Softness, edge, r);
                clip(body * _Life - 0.004);

                // Animated bubbling: two scrolling noise fields beating against each other.
                float t = _Time.y * _BubbleSpeed;
                float n1 = vnoise(IN.uv * _BubbleScale + float2(0.0, t));
                float n2 = vnoise(IN.uv * (_BubbleScale * 0.6) - float2(t * 0.7, 0.0));
                float bubbles = saturate(pow(saturate(n1 * n2), 2.0) * 2.4);

                // Bright corrosive rim hugging the irregular edge.
                float rim = smoothstep(edge - _RimWidth, edge, r) * body;

                float3 col = _BaseColor.rgb;
                col = lerp(col, _HotColor.rgb, bubbles);
                col = lerp(col, _EdgeColor.rgb, rim);

                float grit = lerp(0.85, 1.0, hash(floor(IN.uv * 40.0) + s));
                float a = body * _BaseColor.a * grit;
                a = saturate(a + rim * 0.4 + bubbles * 0.15);
                a *= _Life;

                return half4(col, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
