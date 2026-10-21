Shader "Sailing/Seabed"
{
    Properties
    {
        _Color ("Color", Color) = (0.55, 0.62, 0.42, 1)
        _Caustic ("Caustic", Color) = (0.75, 0.95, 0.85, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Caustic;
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
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(normalWS, mainLight.direction)) * 0.65 + 0.35;
                float2 uv = input.positionWS.xz * 0.28;
                float c1 = sin(uv.x * 6.0 + _Time.y * 1.1) * sin(uv.y * 5.0 - _Time.y * 0.8);
                float c2 = sin((uv.x - uv.y) * 8.0 + _Time.y * 0.6);
                float caustic = pow(saturate(c1) * saturate(c2), 2.0);
                float underwater = saturate(1.0 - input.positionWS.y);
                float3 col = _Color.rgb * ndotl + _Caustic.rgb * caustic * underwater;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
