Shader "Sprites/WaveLightRadial"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _CenterAlpha ("Center Alpha", Range(0, 1)) = 0.157
        _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _RendererColor;
            float _CenterAlpha;

            v2f vert(appdata_t inputVertex)
            {
                v2f outputVertex;
                outputVertex.vertex = UnityObjectToClipPos(inputVertex.vertex);
                outputVertex.texcoord = inputVertex.texcoord;
                outputVertex.color = inputVertex.color * _RendererColor;
                return outputVertex;
            }

            fixed4 frag(v2f inputFragment) : SV_Target
            {
                float2 centeredUv = inputFragment.texcoord - float2(0.5, 0.5);
                float radialDistance = length(centeredUv) * 2.0;

                if (radialDistance > 1.0)
                {
                    discard;
                }

                float radialAlpha = _CenterAlpha * (1.0 - saturate(radialDistance));

                fixed4 finalColor;
                finalColor.rgb = _Color.rgb * inputFragment.color.rgb;
                finalColor.a = radialAlpha * _Color.a * inputFragment.color.a;
                return finalColor;
            }
            ENDCG
        }
    }
}
