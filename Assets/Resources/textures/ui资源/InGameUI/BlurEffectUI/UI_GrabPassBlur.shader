Shader "UI/GrabPassBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Mask", 2D) = "white" {}

        _BlurSize ("Blur Size", Range(0, 12)) = 4
        _EffectAlpha ("Effect Alpha", Range(0, 1)) = 1

        _TintColor ("Tint Color", Color) = (0.75, 1, 0.75, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        // 抓取当前已经渲染好的画面。
        // 因为 BlurEffectUI 在 Hud 后面，所以这里能抓到场景 + Hud。
        GrabPass
        {
            "_UIBlurGrabTexture"
        }

        Pass
        {
            Cull Off
            Lighting Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _UIBlurGrabTexture;
            float4 _UIBlurGrabTexture_TexelSize;

            float _BlurSize;
            float _EffectAlpha;

            float4 _TintColor;
            float _TintStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                o.color = v.color;

                return o;
            }

            fixed4 SampleGrab(v2f i, float2 offset)
            {
                float4 grabPosition = i.grabPos;

                // tex2Dproj 会除以 w，所以这里偏移量要乘 w
                grabPosition.xy += offset * grabPosition.w;

                return tex2Dproj(
                    _UIBlurGrabTexture,
                    UNITY_PROJ_COORD(grabPosition)
                );
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mask = tex2D(_MainTex, i.uv) * i.color;

                float2 texel = _UIBlurGrabTexture_TexelSize.xy * _BlurSize;

                // 13 次采样，强度比简单遮罩明显很多
                fixed4 col = SampleGrab(i, float2(0, 0)) * 0.16;

                col += SampleGrab(i, float2( texel.x, 0)) * 0.10;
                col += SampleGrab(i, float2(-texel.x, 0)) * 0.10;
                col += SampleGrab(i, float2(0,  texel.y)) * 0.10;
                col += SampleGrab(i, float2(0, -texel.y)) * 0.10;

                col += SampleGrab(i, float2( texel.x,  texel.y)) * 0.07;
                col += SampleGrab(i, float2(-texel.x,  texel.y)) * 0.07;
                col += SampleGrab(i, float2( texel.x, -texel.y)) * 0.07;
                col += SampleGrab(i, float2(-texel.x, -texel.y)) * 0.07;

                col += SampleGrab(i, float2( texel.x * 2, 0)) * 0.04;
                col += SampleGrab(i, float2(-texel.x * 2, 0)) * 0.04;
                col += SampleGrab(i, float2(0,  texel.y * 2)) * 0.04;
                col += SampleGrab(i, float2(0, -texel.y * 2)) * 0.04;

                // 中毒色调
                col.rgb = lerp(
                    col.rgb,
                    col.rgb * _TintColor.rgb,
                    _TintStrength
                );

                // 这个 Alpha 很关键：
                // 接近 1 时，清晰画面会被真正挡住，只剩模糊后的画面。
                col.a = saturate(_EffectAlpha * mask.a);

                return col;
            }

            ENDCG
        }
    }
}