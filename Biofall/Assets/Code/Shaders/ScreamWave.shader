Shader "Biofall/ScreamWave"
{
    // Pulsing dark-red shock rings for the Screamer's wave. Unlit, transparent, additive-ish.
    // Driven by _Progress (0..1) which ScreamWaveVFX animates over the wave's lifetime: rings travel
    // outward and the whole thing fades out as Progress approaches 1. Meant for a flat ground quad.
    Properties
    {
        _Color        ("Ring Color", Color) = (0.55, 0.0, 0.0, 1.0)
        _RingCount    ("Ring Count", Float) = 4
        _RingSharpness("Ring Sharpness", Range(1, 16)) = 6
        _ScrollSpeed  ("Outward Scroll Speed", Float) = 1.5
        _PulseSpeed   ("Pulse Speed", Float) = 8
        _Progress     ("Progress (0..1)", Range(0,1)) = 0
        _Intensity    ("Intensity", Float) = 1.6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha One          // additive-style glow so dark red reads on a dark scene
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            fixed4 _Color;
            float  _RingCount;
            float  _RingSharpness;
            float  _ScrollSpeed;
            float  _PulseSpeed;
            float  _Progress;
            float  _Intensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Radial distance from the quad centre, 0 at middle -> 1 at the inscribed circle edge.
                float2 c = i.uv - 0.5;
                float r = length(c) * 2.0;

                // Clip to a circle so the quad's square corners never show.
                if (r > 1.0) discard;

                // Travelling concentric rings: a sawtooth in r that scrolls outward with progress.
                float bands = frac(r * _RingCount - _Progress * _ScrollSpeed);
                // Sharp bright lines from the band centre.
                float ring = pow(1.0 - abs(bands * 2.0 - 1.0), _RingSharpness);

                // Pulse the whole wave so it throbs.
                float pulse = 0.65 + 0.35 * sin(_Progress * _PulseSpeed * 6.2831853);

                // Fade: appear instantly, stay bright nearly the whole life, fade out only at the end.
                float lifeFade = smoothstep(0.0, 0.04, _Progress) * (1.0 - smoothstep(0.85, 1.0, _Progress));
                float edgeFade = smoothstep(1.0, 0.78, r);       // soften the outer rim

                float a = ring * pulse * lifeFade * edgeFade;

                fixed3 col = _Color.rgb * _Intensity;
                return fixed4(col, saturate(a));
            }
            ENDCG
        }
    }
    Fallback Off
}
