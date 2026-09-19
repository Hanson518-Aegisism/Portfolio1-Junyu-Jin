Shader "UI/NightVisionMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Open ("Open", Range(0, 1)) = 0
        _Drop ("Drop", Range(0, 1)) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Open;
            float _Drop;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.texcoord;
                p.y -= _Drop;
                if (p.x < 0.0 || p.x > 1.0 || p.y < 0.0 || p.y > 1.0)
                    return fixed4(0, 0, 0, 0);

                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 scale = float2(aspect, 1.0);
                float radius = _Open * 0.22 * aspect;
                float d1 = distance((i.texcoord - float2(0.33, 0.5)) * scale, 0);
                float d2 = distance((i.texcoord - float2(0.67, 0.5)) * scale, 0);
                float d = min(d1, d2);

                float rimW = 0.018 * aspect;
                float inside = 1.0 - smoothstep(radius - 0.004, radius + 0.004, d);
                float rim = smoothstep(radius + rimW, radius, d) * (1.0 - inside);

                float3 rgb = lerp(float3(0, 0, 0), float3(0.15, 0.55, 0.22), saturate(rim));
                float alpha = (1.0 - inside) * i.color.a;
                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
