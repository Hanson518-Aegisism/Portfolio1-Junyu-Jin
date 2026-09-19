Shader "NightVision/Highlight"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.85, 0.15, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Overlay" }

        Pass
        {
            Name "NightVisionHighlight"
            ZWrite Off
            ZTest Always
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float eyeDepth : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                output.eyeDepth = -vertexInput.positionVS.z;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.screenPos.xy / input.screenPos.w;
                float sceneRaw = SampleSceneDepth(uv);
                float sceneEye = LinearEyeDepth(sceneRaw, _ZBufferParams);
                if (sceneRaw > 0.0001 && input.eyeDepth > sceneEye + 0.2)
                    discard;

                return _Color;
            }
            ENDHLSL
        }
    }
}
