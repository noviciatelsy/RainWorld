Shader "RainWorld/Water/Body"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.35, 0.78, 0.92, 0.28)
        _DeepColor ("Deep Color", Color) = (0.05, 0.18, 0.32, 0.62)
        _SurfaceHighlight ("Surface Highlight", Color) = (0.75, 0.95, 1.0, 0.45)
        _SurfaceLineWidth ("Surface Line Width", Range(0.005, 0.25)) = 0.045
        _SurfaceWaveAmplitude ("Surface Wave Amplitude", Range(0, 0.15)) = 0.035
        _SurfaceWaveSpeed ("Surface Wave Speed", Range(0, 4)) = 1.1
        _CausticsStrength ("Caustics Strength", Range(0, 1)) = 0.28
        _CausticsScale ("Caustics Scale", Range(0.1, 4)) = 0.55
        _CausticsSpeed ("Caustics Speed", Range(0, 2)) = 0.35
        _FoamStrength ("Foam Strength", Range(0, 1)) = 0.22
        _FoamBandWidth ("Foam Band Width", Range(0.01, 0.35)) = 0.09
        _RippleTex ("Ripple Texture", 2D) = "gray" {}
        _RippleBounds ("Ripple Bounds", Vector) = (0, 0, 1, 1)
        _RippleDisplacementStrength ("Ripple Displacement Strength", Range(0, 0.25)) = 0.07
        _RippleLineStrength ("Ripple Line Strength", Range(0, 1)) = 0.42
        _RippleShallowDepth ("Ripple Shallow Depth", Range(0.05, 1.5)) = 0.35
        _VolumeBounds ("Volume Bounds", Vector) = (0, 0, 1, 1)
        _SurfaceY ("Surface Y", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "WaterBodyUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_RippleTex);
            SAMPLER(sampler_RippleTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _SurfaceHighlight;
                float _SurfaceLineWidth;
                float _SurfaceWaveAmplitude;
                float _SurfaceWaveSpeed;
                float _CausticsStrength;
                float _CausticsScale;
                float _CausticsSpeed;
                float _FoamStrength;
                float _FoamBandWidth;
                float4 _RippleBounds;
                float _RippleDisplacementStrength;
                float _RippleLineStrength;
                float _RippleShallowDepth;
                float4 _VolumeBounds;
                float _SurfaceY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    value += amplitude * ValueNoise(p * frequency);
                    frequency *= 2.05;
                    amplitude *= 0.5;
                }

                return value;
            }

            float SurfaceWaveOffset(float worldX, float time)
            {
                float waveSpeed = time * _SurfaceWaveSpeed;
                return sin(worldX * 0.42 + waveSpeed * 1.0) * 0.55
                     + sin(worldX * 0.93 + waveSpeed * 1.65) * 0.30
                     + sin(worldX * 1.85 + waveSpeed * 0.55) * 0.15;
            }

            float SampleCaustics(float2 worldXZ, float time)
            {
                float2 uv = worldXZ * _CausticsScale;
                float t = time * _CausticsSpeed;

                float2 scrollA = float2(t * 0.35, t * 0.18);
                float2 scrollB = float2(-t * 0.22, t * 0.31);

                float layerA = abs(Fbm(uv + scrollA) - Fbm(uv * 1.07 + scrollA + 0.37));
                float layerB = abs(Fbm(uv * 1.35 + scrollB) - Fbm(uv * 0.92 + scrollB + 1.13));

                float caustics = layerA * 0.55 + layerB * 0.45;
                return smoothstep(0.08, 0.72, caustics);
            }

            float SampleFoam(float2 worldPos, float surfaceY, float time)
            {
                float depthFromSurface = surfaceY - worldPos.y;
                if (depthFromSurface <= 0.0 || depthFromSurface > _FoamBandWidth)
                {
                    return 0.0;
                }

                float band = 1.0 - saturate(depthFromSurface / max(_FoamBandWidth, 0.001));
                float foamNoise = Fbm(worldPos * 1.35 + float2(time * 0.45, time * 0.2));
                float foamBreakup = smoothstep(0.42, 0.78, foamNoise);
                return band * foamBreakup;
            }

            float2 RippleUvFromWorldX(float worldX)
            {
                float width = max(_RippleBounds.z, 0.001);
                float u = saturate((worldX - _RippleBounds.x) / width);
                return float2(u, 0.5);
            }

            float SampleRippleDisplacement(float worldX)
            {
                float rippleSample = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, RippleUvFromWorldX(worldX)).r;
                return (rippleSample - 0.5) * _RippleDisplacementStrength;
            }

            float SampleRippleLineWave(float worldX, float depthFromAnimatedSurface)
            {
                float width = max(_RippleBounds.z, 0.001);
                float texel = 1.0 / 256.0;
                float2 rippleUv = RippleUvFromWorldX(worldX);

                float center = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, rippleUv).r - 0.5;
                float left = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, rippleUv - float2(texel, 0.0)).r - 0.5;
                float right = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, rippleUv + float2(texel, 0.0)).r - 0.5;

                float crest = abs(center - left) + abs(center - right);
                float shallowBand = saturate((_RippleShallowDepth - depthFromAnimatedSurface) / max(_RippleShallowDepth, 0.001));
                return crest * shallowBand * _RippleLineStrength;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.worldPos);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y;

                float volumeHeight = max(_VolumeBounds.w - _VolumeBounds.y, 0.001);
                float ambientWaveOffset = SurfaceWaveOffset(input.worldPos.x, time) * _SurfaceWaveAmplitude;
                float rippleOffset = SampleRippleDisplacement(input.worldPos.x);
                float animatedSurfaceY = _SurfaceY + ambientWaveOffset + rippleOffset;

                float depthBelowSurface = saturate((animatedSurfaceY - input.worldPos.y) / volumeHeight);
                depthBelowSurface = pow(depthBelowSurface, 0.75);

                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthBelowSurface);

                float shallowMask = pow(1.0 - depthBelowSurface, 1.35);
                float caustics = SampleCaustics(input.worldPos.xy, time);
                caustics *= shallowMask * _CausticsStrength;
                waterColor.rgb += half3(0.35, 0.55, 0.45) * caustics;
                waterColor.a = saturate(waterColor.a + caustics * 0.08);

                float depthFromAnimatedSurface = animatedSurfaceY - input.worldPos.y;
                float lineWave = SampleRippleLineWave(input.worldPos.x, depthFromAnimatedSurface);
                waterColor.rgb += half3(0.82, 0.95, 1.0) * lineWave;
                waterColor.a = saturate(waterColor.a + lineWave * 0.18);

                float foam = SampleFoam(input.worldPos.xy, animatedSurfaceY, time) * _FoamStrength;
                waterColor.rgb = lerp(waterColor.rgb, half3(0.92, 0.98, 1.0), foam * 0.85);
                waterColor.a = saturate(waterColor.a + foam * 0.25);

                float surfaceDistance = abs(input.worldPos.y - animatedSurfaceY);
                half surfaceLine = half(smoothstep(_SurfaceLineWidth, 0.0, surfaceDistance));
                surfaceLine = saturate(surfaceLine + lineWave * 0.65);
                waterColor.rgb = lerp(waterColor.rgb, _SurfaceHighlight.rgb, surfaceLine * _SurfaceHighlight.a);
                waterColor.a = saturate(waterColor.a + surfaceLine * _SurfaceHighlight.a * 0.35);

                return waterColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
