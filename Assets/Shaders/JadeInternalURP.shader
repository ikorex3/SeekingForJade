Shader "SeekingForJade/JadeInternalURP"
{
    Properties
    {
        _JadePrimaryColor("Jade Primary Color", Color) = (0.15, 0.78, 0.32, 1.0)
        _JadeVeinColor("Jade Vein Color", Color) = (0.05, 0.45, 0.18, 1.0)
        _JadeTranslucency("Translucency", Range(0.0, 1.0)) = 0.65
        _JadePurity("Purity", Range(0.0, 1.0)) = 0.8
        _JadeCracks("Crack Severity", Range(0.0, 1.0)) = 0.1
        _Smoothness("Surface Polish Smoothness", Range(0.0, 1.0)) = 0.92
        _SubsurfaceColor("Subsurface Glow Color", Color) = (0.3, 0.9, 0.5, 1.0)
        _VeinScale("Vein Noise Scale", Float) = 8.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _JadePrimaryColor;
                float4 _JadeVeinColor;
                float4 _SubsurfaceColor;
                float _JadeTranslucency;
                float _JadePurity;
                float _JadeCracks;
                float _Smoothness;
                float _VeinScale;
            CBUFFER_END

            // Procedural noise functions for internal jade crystal grain & veins
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                float2 shift = float2(100.0, 100.0);
                for (int i = 0; i < 4; ++i)
                {
                    v += a * noise(p);
                    p = p * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);

                // Procedural internal jade patterns (floating green veins & cloud cotton)
                float veinNoise = fbm(input.uv * _VeinScale);
                float crystalGrain = noise(input.uv * _VeinScale * 4.0);

                // Impurity / cotton spots based on purity
                float cotton = fbm(input.uv * _VeinScale * 2.5);
                float cottonMask = smoothstep(_JadePurity, 1.0, cotton) * (1.0 - _JadePurity);

                // Blend colors: base -> vein -> cotton
                float3 albedo = lerp(_JadePrimaryColor.rgb, _JadeVeinColor.rgb, veinNoise);
                albedo = lerp(albedo, float3(0.9, 0.95, 0.9), cottonMask * 0.7);

                // Crack lines based on crack severity
                float crackNoise = frac(fbm(input.uv * _VeinScale * 3.0) * 12.0);
                float crackMask = smoothstep(0.05 * _JadeCracks, 0.0, abs(crackNoise - 0.5));
                albedo = lerp(albedo, float3(0.2, 0.18, 0.15), crackMask * _JadeCracks);

                // Lighting
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float NdotL = saturate(dot(normalWS, mainLight.direction));

                // Pseudo-subsurface scattering (light wrap-around through translucent jade)
                float sssWrap = saturate((dot(normalWS, mainLight.direction) + 0.4) / 1.4);
                float3 sssColor = _SubsurfaceColor.rgb * _JadeTranslucency * sssWrap * mainLight.color;

                // Backlight translucency (light shining from behind the rock)
                float backLight = saturate(dot(-mainLight.direction, viewDirWS)) * _JadeTranslucency;
                float3 backSss = _JadePrimaryColor.rgb * backLight * mainLight.color * 1.5;

                // Specular reflection for polished cut face
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specularPower = exp2(10.0 * _Smoothness + 1.0);
                float specular = pow(NdotH, specularPower) * _Smoothness;

                // Fresnel edge glow (vitreous / glassy lustre of genuine jade)
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 4.0);
                float3 ambient = SampleSH(normalWS) * albedo;

                float3 diffuse = albedo * mainLight.color * NdotL;
                float3 finalColor = diffuse + sssColor * 0.5 + backSss + specular * mainLight.color + fresnel * 0.15 * _JadePrimaryColor.rgb + ambient;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
