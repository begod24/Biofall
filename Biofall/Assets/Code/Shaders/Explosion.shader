Shader "Biofall/Explosion"
{
    // Custom URP additive fireball: procedural value-noise ramp (hot core -> orange -> red edge),
    // fresnel rim glow and a noise dissolve driven by _Progress (0 = full ball, 1 = burnt out).
    // Animated from the Explosion.cs controller via a MaterialPropertyBlock (_Progress + transform scale).
    Properties
    {
        _ColorHot  ("Hot Core",   Color) = (1.0, 0.95, 0.65, 1)
        _ColorMid  ("Mid Flame",  Color) = (1.0, 0.45, 0.10, 1)
        _ColorEdge ("Edge Ember", Color) = (0.75, 0.07, 0.03, 1)
        _Progress  ("Progress", Range(0,1)) = 0
        _NoiseScale("Noise Scale", Float) = 3.0
        _Fresnel   ("Fresnel Power", Float) = 2.0
        _Emission  ("Emission Boost", Float) = 4.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ExplosionFireball"
            Blend One One      // additive — glows, no depth-sort needed
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
            };

            float4 _ColorHot, _ColorMid, _ColorEdge;
            float _Progress, _NoiseScale, _Fresnel, _Emission;

            // --- cheap value noise (hash + trilinear) ---
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            float vnoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash(i + float3(0,0,0));
                float n100 = hash(i + float3(1,0,0));
                float n010 = hash(i + float3(0,1,0));
                float n110 = hash(i + float3(1,1,0));
                float n001 = hash(i + float3(0,0,1));
                float n101 = hash(i + float3(1,0,1));
                float n011 = hash(i + float3(0,1,1));
                float n111 = hash(i + float3(1,1,1));
                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }
            float fbm(float3 p)
            {
                float v = 0.0, a = 0.5;
                [unroll] for (int i = 0; i < 3; i++) { v += a * vnoise(p); p *= 2.0; a *= 0.5; }
                return v;
            }

            Varyings vert (Attributes IN)
            {
                Varyings o;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                o.positionHCS = vp.positionCS;
                o.positionOS  = IN.positionOS.xyz;
                o.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                o.viewDirWS   = GetWorldSpaceViewDir(vp.positionWS);
                return o;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // animated turbulence over the surface
                float3 np = IN.positionOS * _NoiseScale + float3(0, -_Progress * 2.0, 0);
                float n = fbm(np + _Progress * 1.5);

                // dissolve: as progress climbs, low-noise areas burn away first
                float burn = n - _Progress * 1.25;
                clip(burn + 0.15);

                // colour ramp hot -> mid -> edge by noise
                float3 col = lerp(_ColorHot.rgb, _ColorMid.rgb, saturate(n * 1.6));
                col = lerp(col, _ColorEdge.rgb, saturate((n - 0.5) * 2.0));

                // fresnel rim brightening
                float3 V = normalize(IN.viewDirWS);
                float fres = pow(1.0 - saturate(dot(normalize(IN.normalWS), V)), _Fresnel);
                col += _ColorMid.rgb * fres;

                // brightness envelope: flares up fast, fades to ember
                float envelope = (1.0 - _Progress) * saturate(_Progress * 6.0);
                col *= _Emission * envelope;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
