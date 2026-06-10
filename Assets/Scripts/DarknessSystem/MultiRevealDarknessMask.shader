Shader "UI/MultiRevealDarknessMask"
{
    Properties
    {
        // 黑暗遮罩的颜色与透明度。
        _DarknessColor("Darkness Color", Color) = (0, 0, 0, 1)
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
        ZTest Always

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            /*
             * 必须与 DarknessMaskController 中的最大数量保持一致。
             * 数量越高，可以同时显示的可视源越多，
             * 但全屏 Shader 的计算量也会随之增加。
             */
            #define MAX_REVEAL_SOURCES 32

            struct AppData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexToFragment
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _DarknessColor;

            // 当前实际参与计算的可视源数量。
            int _RevealCount;

            /*
             * 每个 Vector 保存一个可视源：
             *
             * x = 屏幕视口坐标 X
             * y = 屏幕视口坐标 Y
             * z = 可视半径
             * w = 边缘柔和范围
             */
            float4 _RevealSources[MAX_REVEAL_SOURCES];

            VertexToFragment vert(AppData input)
            {
                VertexToFragment output;

                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;

                return output;
            }

            fixed4 frag(VertexToFragment input) : SV_Target
            {
                /*
                 * 默认完全显示黑色遮罩。
                 * 只要当前像素落入任意一个可视源，
                 * darkness 就会逐渐接近 0。
                 */
                float darkness = 1.0;

                // 修正屏幕宽高比，防止圆形在宽屏中变成椭圆。
                float aspectRatio =
                    _ScreenParams.x / max(_ScreenParams.y, 1.0);

                for (int i = 0; i < _RevealCount; i++)
                {
                    float4 source = _RevealSources[i];

                    float2 offset =
                        input.uv - source.xy;

                    offset.x *= aspectRatio;

                    float distanceFromSource =
                        length(offset);

                    float radius =
                        max(source.z, 0.0);

                    float softness =
                        max(source.w, 0.0001);

                    /*
                     * 圆内为 0，代表遮罩完全透明。
                     * 柔和区域逐渐从 0 过渡到 1。
                     * 圆外为 1，代表完整显示黑色。
                     */
                    float sourceDarkness = smoothstep(
                        radius,
                        radius + softness,
                        distanceFromSource
                    );

                    /*
                     * 取所有可视源中最透明的结果。
                     * 因此只要处于任意一个圆形范围内，就能看到场景。
                     */
                    darkness = min(
                        darkness,
                        sourceDarkness
                    );
                }

                return fixed4(
                    _DarknessColor.rgb,
                    _DarknessColor.a * darkness
                );
            }

            ENDCG
        }
    }
}