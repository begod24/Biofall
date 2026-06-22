Shader "Biofall/CRTOverlay"
{
    // Full-screen UI overlay (RawImage). Draws a procedural CRT look ON TOP of the menu:
    // scanlines, a slowly rolling sync band, gentle flicker, vignette and faint noise.
    // It only adds its own color (cannot read the screen behind it), so the effect is a
    // tasteful, continuous "signal" film rather than true pixel distortion.
    Properties
    {
        // UI (RawImage/CanvasRenderer) expects a _MainTex property even if unused.
        _MainTex      ("Texture (unused)",   2D)        = "white" {}
        _ScanCount    ("Scanline Count",     Float)     = 650
        _ScanStrength ("Scanline Strength",  Range(0,1)) = 0.14
        _BandSpeed    ("Band Speed",         Float)     = 0.12
        _BandStrength ("Band Strength",      Range(0,1)) = 0.10
        _BandWidth    ("Band Width",         Range(0,1)) = 0.18
        _Flicker      ("Flicker",            Range(0,1)) = 0.035
        _Vignette     ("Vignette",           Range(0,3)) = 0.7
        _Noise        ("Noise",              Range(0,1)) = 0.035
        _Tint         ("Highlight Tint",     Color)     = (0.75, 0.82, 1.0, 1.0)
        _Master       ("Master Opacity",     Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            float _ScanCount, _ScanStrength, _BandSpeed, _BandStrength, _BandWidth;
            float _Flicker, _Vignette, _Noise, _Master;
            float4 _Tint;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            Varyings vert(Attributes IN)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.uv = IN.uv;
                o.color = IN.color;
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float t = _Time.y;

                // Horizontal scanlines (darken every other line).
                float scan = sin(uv.y * _ScanCount * 3.14159265) * 0.5 + 0.5;
                float darkScan = scan * _ScanStrength;

                // Slowly rolling sync band (wrapped vertical distance).
                float bandPos = frac(t * _BandSpeed);
                float d = abs(frac(uv.y - bandPos + 0.5) - 0.5);
                float band = smoothstep(_BandWidth, 0.0, d) * _BandStrength;

                // Vignette: darken toward the edges/corners.
                float2 c = uv - 0.5;
                float vig = saturate(dot(c, c) * _Vignette);

                // Faint animated noise.
                float n = (hash(uv * float2(800.0, 600.0) + t) - 0.5) * _Noise;

                // Subtle global flicker.
                float fl = (sin(t * 50.0) * 0.5 + 0.5) * _Flicker;

                // Total darkening; the bright band lifts it a touch.
                float darkA = saturate(darkScan + vig + fl + n - band * 0.5);

                half3 col = half3(0.0, 0.0, 0.0);
                col = lerp(col, _Tint.rgb, saturate(band));
                half a = saturate(darkA + band * 0.5);

                a *= _Master * IN.color.a;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
