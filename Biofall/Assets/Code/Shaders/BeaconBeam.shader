Shader "Biofall/BeaconBeam"
{
    // Vertical column of red signal light for the Mission 1 beacon. Additive, unlit.
    // A fresnel "tube" rim makes the cylinder read as a hollow shaft of light; alpha
    // fades toward the top; scrolling value-noise + a time pulse give it life.
    // Meant for a tall thin cylinder. Animated entirely in-shader (no script needed).
    Properties
    {
        _ColorCore ("Core Color", Color) = (1.0, 0.15, 0.10, 1)
        _ColorEdge ("Edge Color", Color) = (1.0, 0.35, 0.20, 1)
        _Emission  ("Emission Boost", Float) = 3.0
        _Fresnel   ("Fresnel Power", Float) = 1.6
        _TopFade   ("Top Fade", Float) = 1.0
        _ScrollSpeed ("Scroll Speed", Float) = 1.5
        _NoiseScale  ("Noise Scale", Float) = 4.0
        _PulseSpeed  ("Pulse Speed", Float) = 2.5
        _PulseAmount ("Pulse Amount", Range(0,1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "BeaconBeam"
            Blend One One   // additive glow
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float2 uv          : TEXCOORD3;
            };

            float4 _ColorCore, _ColorEdge;
            float _Emission, _Fresnel, _TopFade, _ScrollSpeed, _NoiseScale, _PulseSpeed, _PulseAmount;

            float hash(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            float vnoise(float2 x)
            {
                float2 i = floor(x);
                float2 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert (Attributes IN)
            {
                Varyings o;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                o.positionHCS = vp.positionCS;
                o.positionOS  = IN.positionOS.xyz;
                o.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                o.viewDirWS   = GetWorldSpaceViewDir(vp.positionWS);
                o.uv          = IN.uv;
                return o;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // height 0 at base, 1 at top (unit cylinder spans y -0.5..0.5)
                float h = saturate(IN.positionOS.y + 0.5);

                // scrolling noise rising up the shaft
                float n = vnoise(float2(IN.uv.x * _NoiseScale, IN.uv.y * _NoiseScale - _Time.y * _ScrollSpeed));

                // fresnel tube: bright at grazing angles → looks like a hollow beam
                float3 V = normalize(IN.viewDirWS);
                float fres = pow(1.0 - saturate(abs(dot(normalize(IN.normalWS), V))), _Fresnel);

                float3 col = lerp(_ColorCore.rgb, _ColorEdge.rgb, fres);
                col += _ColorEdge.rgb * fres;

                // brightness: noise sparkle + fade to top + alert pulse
                float pulse = 1.0 - _PulseAmount + _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed));
                float topFade = pow(1.0 - h, _TopFade);
                float a = (0.35 + 0.65 * n) * topFade * fres * pulse;

                col *= _Emission * a;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
