Shader "Custom/RenderMeshUnlit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaTest("Alpha Testing", Float) = 0.0
        _FillAmount("Fill Amount", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 4.5
            #pragma multi_compile_instancing
            
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #pragma multi_compile_fog

            StructuredBuffer<float4x4> _Matrices;

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _FillAmount;
            CBUFFER_END
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                uint instanceID     : SV_InstanceID;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float positionOSY   : TEXCOORD2;
                half fogFactor      : TEXCOORD4;
                float4 positionCS   : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                float4x4 matri = _Matrices[IN.instanceID];
                
                OUT.positionWS = mul(matri, float4(IN.positionOS.xyz, 1.0)).xyz;
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                OUT.positionOSY = IN.positionOS.y;
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                half3 finalColor = albedo.rgb;

                finalColor = MixFog(finalColor, IN.fogFactor);
                clip(-IN.positionOSY + 2 * (_FillAmount) - 1);
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}