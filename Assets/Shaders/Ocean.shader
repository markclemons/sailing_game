Shader "Sailing/Ocean"
{
    Properties
    {
        _Shallow ("Shallow", Color) = (0.20, 0.78, 0.72, 0.55)
        _Deep ("Deep", Color) = (0.02, 0.16, 0.38, 0.92)
        _Foam ("Foam", Color) = (0.86, 0.95, 0.95, 1)
        _Sand ("Sand", Color) = (0.62, 0.52, 0.32, 1)
        _FoamWidth ("Foam Width", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Shallow;
                half4 _Deep;
                half4 _Foam;
                half4 _Sand;
                float _FoamWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fog : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(p.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneEye = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float surfaceEye = -TransformWorldToView(input.positionWS).z;
                float thickness = max(0.0, sceneEye - surfaceEye);

                float depthT = saturate(thickness / 18.0);
                float3 col = lerp(_Shallow.rgb, _Deep.rgb, depthT);
                float seeBed = saturate(1.0 - thickness / 7.0);
                col = lerp(col, _Sand.rgb, seeBed * 0.72);

                float2 cuv = input.positionWS.xz * 0.32;
                float c1 = sin(cuv.x * 5.0 + _Time.y * 1.3) * sin(cuv.y * 4.2 - _Time.y);
                float c2 = sin((cuv.x + cuv.y) * 6.5 - _Time.y * 0.7);
                float caustic = pow(saturate(c1 * 0.5 + 0.5) * saturate(c2 * 0.5 + 0.5), 3.0);
                col += caustic * seeBed * float3(0.28, 0.38, 0.28);

                float shore = 1.0 - smoothstep(0.2, _FoamWidth, thickness);
                float crest = saturate((0.94 - normalWS.y) * 10.0);
                float foam = saturate(shore + crest * 0.65);
                col = lerp(col, _Foam.rgb, foam);

                float3 view = normalize(_WorldSpaceCameraPos - input.positionWS);
                Light mainLight = GetMainLight();
                float3 halfDir = normalize(mainLight.direction + view);
                float spec = pow(saturate(dot(normalWS, halfDir)), 160.0);
                col += spec * mainLight.color.rgb * 0.7;

                float fresnel = pow(1.0 - saturate(dot(normalWS, view)), 5.0);
                float3 refl = reflect(-view, normalWS);
                float3 sky = lerp(float3(0.55, 0.76, 0.78), float3(0.25, 0.48, 0.82), saturate(refl.y));
                float3 env = GlossyEnvironmentReflection(refl, 0.02, 1.0);
                col = lerp(col, lerp(sky, env, 0.65), fresnel * 0.62);

                float alpha = lerp(0.42, 0.94, saturate(thickness / 10.0));
                alpha = lerp(alpha, 0.95, foam * 0.5);
                col = MixFog(col, input.fog);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
