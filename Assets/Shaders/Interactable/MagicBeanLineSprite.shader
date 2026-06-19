Shader "Interactable/MagicBeanLineSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _SpriteRect ("Sprite Rect (offset.xy, scale.zw)", Vector) = (0, 0, 1, 1)
        _TileCount ("Tile Count Along Line", Float) = 1
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
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
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _SpriteRect;
            float _TileCount;
            fixed4 _Color;

            v2f vert(appdata inputData)
            {
                v2f outputData;
                outputData.vertex = UnityObjectToClipPos(inputData.vertex);
                outputData.uv = inputData.uv;
                outputData.color = inputData.color * _Color;
                return outputData;
            }

            fixed4 frag(v2f inputData) : SV_Target
            {
                // LineRenderer: uv.x = along line length, uv.y = across line width.
                // Sprite is vertical: map line length -> sprite V (tiled), width -> sprite U.
                float tiledAlongLine = frac(inputData.uv.x * _TileCount);
                float2 spriteUv = float2(
                    _SpriteRect.x + inputData.uv.y * _SpriteRect.z,
                    _SpriteRect.y + tiledAlongLine * _SpriteRect.w
                );

                fixed4 color = tex2D(_MainTex, spriteUv) * inputData.color;
                clip(color.a - 0.001);
                return color;
            }
            ENDCG
        }
    }
}
