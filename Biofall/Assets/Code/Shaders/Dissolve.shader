Shader "Biofall/Dissolve"
{
    // Death-dissolve for skinned enemies. Burns the mesh away by a world-space noise
    // threshold (_DissolveAmount 0->1) with a glowing emissive edge band. Unlit on purpose:
    // it only runs for the brief corpse phase, where the burning edge carries the look.
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _DissolveAmount ("Dissolve", Range(0,1)) = 0
        _EdgeWidth ("Edge Width", Range(0.001,0.4)) = 0.08
        [HDR] _EdgeColor ("Edge Color (HDR)", Color) = (4,1.2,0.25,1)
        _NoiseScale ("Noise Scale", Float) = 9
        _BodyDim ("Body Dim", Range(0.3,1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _EdgeColor;
                float _DissolveAmount;
                float _EdgeWidth;
                float _NoiseScale;
                float _BodyDim;
            CBUFFER_END

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
                return lerp(lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                                 lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                                 lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = p.positionCS;
                OUT.positionWS  = p.positionWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float n = vnoise(IN.positionWS * _NoiseScale);
                float amt = _DissolveAmount;
                clip(n - amt * 1.0001);

                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                col.rgb *= _BodyDim;

                float edge = 1.0 - smoothstep(amt, amt + _EdgeWidth, n);
                col.rgb += _EdgeColor.rgb * edge;
                return half4(col.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
