Shader "Sprites/DissolveBurn"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HDR] _EdgeColor ("Edge Color", Color) = (0.45, 0.9, 1.0, 1)
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 1
        _EdgeWidth ("Edge Width", Range(0.001, 0.3)) = 0.06
        _NoiseScale ("Noise Scale", Float) = 8
        _NoiseTex ("Noise Texture (Optional)", 2D) = "white" {}
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
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            fixed4 _Color;
            fixed4 _EdgeColor;
            float _DissolveAmount;
            float _EdgeWidth;
            float _NoiseScale;

            float Hash21(float2 position)
            {
                return frac(sin(dot(position, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 local = frac(uv);
                local = local * local * (3.0 - 2.0 * local);

                float sampleA = Hash21(cell);
                float sampleB = Hash21(cell + float2(1.0, 0.0));
                float sampleC = Hash21(cell + float2(0.0, 1.0));
                float sampleD = Hash21(cell + float2(1.0, 1.0));

                float blendX = lerp(sampleA, sampleB, local.x);
                float blendY = lerp(sampleC, sampleD, local.x);
                return lerp(blendX, blendY, local.y);
            }

            float SampleDissolveNoise(float2 uv)
            {
                float2 noiseUv = uv * _NoiseScale;
                float proceduralNoise = ValueNoise(noiseUv);
                float textureNoise = tex2D(_NoiseTex, TRANSFORM_TEX(noiseUv, _NoiseTex)).r;
                return saturate(proceduralNoise * textureNoise);
            }

            v2f vert(appdata_t inputVertex)
            {
                v2f outputVertex;

                #ifdef PIXELSNAP_ON
                inputVertex.vertex = UnityPixelSnap(inputVertex.vertex);
                #endif

                outputVertex.vertex = UnityObjectToClipPos(inputVertex.vertex);
                outputVertex.texcoord = inputVertex.texcoord;
                outputVertex.color = inputVertex.color * _Color;
                return outputVertex;
            }

            fixed4 frag(v2f inputFragment) : SV_Target
            {
                fixed4 spriteColor = tex2D(_MainTex, inputFragment.texcoord) * inputFragment.color;
                float dissolveNoise = SampleDissolveNoise(inputFragment.texcoord);

                clip(dissolveNoise - _DissolveAmount);

                // 仅在溶解前沿（噪声刚超过阈值的窄带）叠灼烧色，避免整图被 EdgeColor 覆盖。
                float burnMask = 1.0 - smoothstep(_DissolveAmount, _DissolveAmount + _EdgeWidth, dissolveNoise);
                spriteColor.rgb += _EdgeColor.rgb * burnMask * _EdgeColor.a;

                clip(spriteColor.a - 0.001);

                return spriteColor;
            }
            ENDCG
        }
    }
}
