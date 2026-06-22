Shader "Biofall/Puddle"
{
    // Ground-aligned wet decal used for two things via material params:
    //  • Puddles  — a soft dark "water" fill with a bright wet rim and faint concentric ripples.
    //  • Splashes — fill off (_DiscAlpha 0), just a bright expanding ring (the particle's Size-over-
    //    Lifetime grows the quad, so the ring travels outward like a raindrop impact).
    // Alpha is multiplied by the particle vertex colour, so Color-over-Lifetime fades each one.
    Properties
    {
        _Color       ("Water Tint",  Color) = (0.06, 0.08, 0.12, 1.0)
        _RingColor   ("Sheen/Ripple Color", Color) = (0.55, 0.65, 0.80, 1.0)
        _DiscAlpha   ("Fill Opacity", Range(0,1)) = 0.5
        _RimPos      ("Rim Position", Range(0,1)) = 0.82
        _RimWidth    ("Rim Width", Range(0.01,0.5)) = 0.12
        _RimStrength ("Rim Strength", Range(0,2)) = 0.7
        _RippleStrength ("Ripple Strength", Range(0,1)) = 0.25
        _RippleWidth ("Ripple Width", Range(0.01,0.3)) = 0.06
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            fixed4 _Color, _RingColor;
            float _DiscAlpha, _RimPos, _RimWidth, _RimStrength, _RippleStrength, _RippleWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float band(float r, float pos, float w) { return 1.0 - smoothstep(0.0, w, abs(r - pos)); }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 c = i.uv - 0.5;
                float r = length(c) * 2.0;
                if (r > 1.0) discard;

                // Soft dark water fill, strongest in the middle, gone by the rim.
                float fill = smoothstep(1.0, 0.05, r) * _DiscAlpha;

                // Bright wet rim (meniscus / reflective edge of a puddle).
                float rim = band(r, _RimPos, _RimWidth) * _RimStrength;

                // A few faint concentric ripples (rain disturbing the surface).
                float ripple = (band(r, 0.34, _RippleWidth) + band(r, 0.62, _RippleWidth) + band(r, 0.88, _RippleWidth)) * _RippleStrength;

                float highlight = saturate(rim + ripple);
                fixed3 col = lerp(_Color.rgb, _RingColor.rgb, highlight);

                float edgeFade = smoothstep(1.0, 0.82, r);     // soften the outer circle
                float a = saturate(fill + rim + ripple) * i.color.a * edgeFade;
                return fixed4(col, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
