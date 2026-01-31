Shader "Custom/RenderMeshLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [HDR] _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        
        [Toggle(SPECULARHIGHLIGHTS_OFF)] _SpecularHighlights ("Specular Highlights", Float) = 1.0
        [Toggle(RECEIVE_SHADOWS_OFF)] _ReceiveShadows("Receive Shadows", Float) = 1.0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 4.5
            #pragma multi_compile_instancing
            
            #pragma shader_feature_local SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog

            StructuredBuffer<float4x4> _Matrices;

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _SpecColor;
            half _Smoothness;
            half _Cutoff;
            CBUFFER_END
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                half3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                uint instanceID     : SV_InstanceID;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS      : TEXCOORD2;
                float4 shadowCoord  : TEXCOORD3;
                half fogFactor      : TEXCOORD4;
                float4 positionCS   : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                float4x4 matri = _Matrices[IN.instanceID];
                
                float3 positionWS = mul(matri, float4(IN.positionOS.xyz, 1.0)).xyz;
                float4 positionCS = TransformWorldToHClip(positionWS);
                half3 normalWS = normalize(mul((float3x3)matri, IN.normalOS));
                
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                vpi.positionWS = positionWS;
                vpi.positionCS = positionCS;
                
                OUT.positionCS = positionCS;
                OUT.positionWS = positionWS;
                OUT.normalWS = normalWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                OUT.shadowCoord = GetShadowCoord(vpi);
                OUT.fogFactor = ComputeFogFactor(positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                half3 normalWS = normalize(IN.normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                Light mainLight = GetMainLight(IN.shadowCoord);
                
                half dotNL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuseColor = albedo.rgb * mainLight.color * dotNL;

                half3 halfwayDir = normalize(mainLight.direction + viewDirWS);
                half dotNH = saturate(dot(normalWS, halfwayDir));
                half specPower = exp2(_Smoothness * 10.0) + 2.0;
                half3 specularColor = mainLight.color * _SpecColor.rgb * pow(dotNH, specPower);

                #if defined(SPECULARHIGHLIGHTS_OFF)
                    specularColor = 0.0h;
                #endif
                
                half shadowAttenuation = mainLight.shadowAttenuation;
                #if defined(RECEIVE_SHADOWS_OFF)
                    shadowAttenuation = 1.0h;
                #endif
                
                half3 ambientColor = albedo.rgb * SampleSH(normalWS);
                
                half3 finalColor = ambientColor + (diffuseColor * shadowAttenuation) + (specularColor * shadowAttenuation);
                
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}