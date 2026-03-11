Shader "PolygonArsenal/PolyLitSurface_URP"
{
    Properties
    {
        _GlowIntensity("Glow Intensity", Range(1,5)) = 1
        _Smoothness("Smoothness", Range(0,1)) = 0
        _Metallic("Metallic", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Back

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 color : COLOR;
            };

            float _GlowIntensity;
            float _Metallic;
            float _Smoothness;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color = IN.color;

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float3 albedo = IN.color.rgb;

                float3 emission = IN.color.rgb * _GlowIntensity;

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);

                float NdotL = saturate(dot(normalWS, -lightDir));

                float3 diffuse = albedo * mainLight.color * NdotL;

                float3 color = diffuse + emission;

                return float4(color, 1.0);
            }

            ENDHLSL
        }
    }
}