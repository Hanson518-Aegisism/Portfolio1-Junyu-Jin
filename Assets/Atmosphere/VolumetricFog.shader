Shader "Portfolio/Atmosphere/VolumetricFog"
{
    Properties
    {
        _FogColor("Haze Color", Color) = (0.20,0.235,0.25,1)
        _Density("Density", Range(0,0.08)) = 0.018
        _Brightness("Fog Brightness", Range(0.1,4)) = 1
        _HeightFalloff("Height Falloff", Range(0.05,2)) = 0.24
        _BaseHeight("Base Height", Float) = 0
        _NoiseScale("Noise Scale", Float) = 0.085
        _DriftSpeed("Drift Speed", Float) = 0.12
        _SmokeScale("Smoke Scale", Float) = 0.3
        _SmokeBreakup("Smoke Breakup", Range(0,1)) = 0.9
        _FogTime("Smoke Time", Float) = 0
        _MaxOpacity("Maximum Opacity", Range(0,1)) = 0.60
        _MaxDistance("Maximum Distance", Float) = 110
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-50" "RenderType"="Transparent" }
        Pass
        {
            Name "VolumetricFog"
            Cull Front ZWrite Off ZTest Always
            Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _FogColor;
            float _Density, _Brightness, _HeightFalloff, _BaseHeight, _NoiseScale, _DriftSpeed, _MaxOpacity, _MaxDistance;
            float _SmokeScale, _SmokeBreakup, _FogTime;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes i)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS=TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS); return o;
            }
            float Hash(float3 p) {p=frac(p*0.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
            float Noise(float3 p)
            {
                float3 k=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(lerp(Hash(k),Hash(k+float3(1,0,0)),f.x),lerp(Hash(k+float3(0,1,0)),Hash(k+float3(1,1,0)),f.x),f.y),
                lerp(lerp(Hash(k+float3(0,0,1)),Hash(k+float3(1,0,1)),f.x),lerp(Hash(k+float3(0,1,1)),Hash(k+1),f.x),f.y),f.z);
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);
                float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 surface=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float3 origin=GetCameraPositionWS();
                float3 dir=normalize(i.positionWS-origin);
                if(unity_OrthoParams.w>0.5) {dir=-UNITY_MATRIX_V[2].xyz; origin=surface-dir*dot(surface-origin,dir);}
                float3 ro=TransformWorldToObject(origin), rd=mul((float3x3)unity_WorldToObject,dir);
                rd=sign(rd+1e-8)*max(abs(rd),1e-7);
                float3 a=(-0.5-ro)/rd, b=(0.5-ro)/rd;
                float3 lo=min(a,b), hi=max(a,b);
                float start=max(max(lo.x,max(lo.y,lo.z)),0);
                float end=min(min(hi.x,min(hi.y,hi.z)),min(length(surface-origin),_MaxDistance));
                if(end<=start)return 0;
                float stepSize=(end-start)/60;
                float jitter=Hash(float3(floor(uv*_ScaledScreenParams.xy),0));
                float transmission=1; float3 scattered=0;
                [loop] for(int s=0;s<60;s++)
                {
                    float t=start+(s+jitter)*stepSize;
                    float3 p=origin+dir*t;
                    float3 local=TransformWorldToObject(p);
                    float edge=saturate((0.5-max(abs(local.x),abs(local.z)))*18)*saturate((0.5-abs(local.y))*12);
                    // Advect a genuinely three-dimensional density field in world space.
                    // Broad empty pockets stop distant overlapping smoke from becoming a uniform veil.
                    float clock=_FogTime*_DriftSpeed;
                    float3 q=(p-float3(0.85,0.07,0.4)*clock)*_SmokeScale;
                    float3 warp=float3(Noise(q*0.55+11),Noise(q*0.55+37),Noise(q*0.55+71))-0.5;
                    q+=warp*2.8;
                    q.y+=sin(q.x*0.7+q.z*0.45+clock*0.25)*0.45;
                    float rolling=Noise(q+float3(0,clock*0.07,0))*0.7+Noise(q*2.3-float3(0,clock*0.11,0))*0.3;
                    float pockets=smoothstep(0.34,0.65,Noise(q*0.32+float3(9,3,17)));
                    float clumps=smoothstep(0.40,0.68,rolling)*pockets;
                    float smoke=lerp(0.8,0.025+clumps*9.0,_SmokeBreakup);
                    float density=_Density*exp(-max(p.y-_BaseHeight,0)*_HeightFalloff)*smoke*edge*smoothstep(0.6,2.5,t);
                    Light light=GetMainLight(TransformWorldToShadowCoord(p));
                    float3 illumination=_FogColor.rgb*_Brightness*(0.32+light.color*light.shadowAttenuation*0.65);
                    float opacity=1-exp(-density*stepSize);
                    scattered+=transmission*opacity*illumination;
                    transmission*=1-opacity;
                }
                float alpha=1-transmission;
                float capped=min(alpha,_MaxOpacity);
                return half4(scattered*(capped/max(alpha,0.0001)),capped);
            }
            ENDHLSL
        }
    }
}
