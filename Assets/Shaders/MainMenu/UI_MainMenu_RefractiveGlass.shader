Shader "UI/MainMenu/RefractiveGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.1, 0.1, 0.2, 0.5)
        
        [Header(Crystal Details)]
        _ParallaxStrength ("Internal Parallax", Range(0, 0.1)) = 0.02
        _SparkleIntensity ("Sparkle Intensity", Range(0, 2.0)) = 0.5
        _InclusionIntensity ("Inclusion Intensity", Range(0, 2.0)) = 0.3
        _SparkleScale ("Sparkle Scale", Range(1, 50)) = 10.0
        _InclusionScale ("Inclusion Scale", Range(1, 50)) = 20.0
        _SparkleSharpness ("Sparkle Sharpness", Range(1, 100)) = 40.0
        _SparkleTwinkleSpeed ("Sparkle Twinkle Speed", Range(0, 5.0)) = 2.0
        
        [Header(Glass Effects)]
        _FresnelPower ("Fresnel Power", Range(0.1, 5.0)) = 2.0
        _FresnelIntensity ("Fresnel Intensity", Range(0, 2.0)) = 0.3
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.1)) = 0.02
        _UVDistortion ("UV Distortion Strength", Range(0, 0.3)) = 0.05
        _DistortionSpeed ("Distortion Speed", Range(0, 2.0)) = 0.5
        _RadialGradient ("Radial Gradient Intensity", Range(0, 1.0)) = 0.3
        _RadialFalloff ("Radial Falloff", Range(1, 10)) = 3.0
        
        [Header(Border Settings)]
        _BorderColor ("Border Color", Color) = (0.4, 0.7, 1.0, 1.0)
        _BorderWidth ("Border Width", Range(0, 0.5)) = 0.02
        _BorderGlow ("Border Glow", Float) = 2.0
        _EdgeGlowIntensity ("Edge Glow Intensity", Range(0, 0.5)) = 0.05

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _Color;
            float _ParallaxStrength;
            float _SparkleIntensity;
            float _InclusionIntensity;
            float _SparkleScale;
            float _InclusionScale;
            float _SparkleSharpness;
            float _SparkleTwinkleSpeed;
            float _FresnelPower;
            float _FresnelIntensity;
            float _ChromaticAberration;
            float _UVDistortion;
            float _DistortionSpeed;
            float _RadialGradient;
            float _RadialFalloff;
            float4 _BorderColor;
            float _BorderWidth;
            float _BorderGlow;
            float _EdgeGlowIntensity;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = v.vertex; 
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float smoothNoise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                float v0 = lerp(a, b, u.x);
                float v1 = lerp(c, d, u.x);
                return lerp(v0, v1, u.y);
            }

            float3 getCrystalSparkles(float2 uv, float scale, float speed, float intensity, float sharpness) {
                float2 sUV = uv * scale;
                
                // Multi-octave noise for complex patterns
                float noise1 = smoothNoise(sUV + _Time.y * speed * 0.5);
                float noise2 = smoothNoise(sUV * 2.0 - _Time.y * speed * 0.3) * 0.5;
                float noise3 = smoothNoise(sUV * 0.5 + _Time.y * speed * 0.2) * 0.25;
                
                float combinedNoise = noise1 + noise2 + noise3;
                
                // Create sharp crystal peaks
                float sparkle = pow(max(combinedNoise, 0.0), sharpness);
                
                // Smooth twinkling animation
                float twinkle = abs(sin(_Time.y * _SparkleTwinkleSpeed + sUV.x * 5.0 + sUV.y * 7.0));
                twinkle = smoothstep(0.3, 0.8, twinkle);
                
                sparkle *= twinkle;
                return sparkle * intensity;
            }

            float4 sampleWithChromaticAberration(float2 uv) {
                float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(_ChromaticAberration, 0)).r;
                float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(_ChromaticAberration, 0)).b;
                float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                return float4(r, g, b, a);
            }

            float2 getUVDistortion(float2 uv) {
                float distortion = smoothNoise(uv * 3.0 + _Time.y * _DistortionSpeed) - 0.5;
                distortion += smoothNoise(uv * 7.0 - _Time.y * _DistortionSpeed * 0.5) * 0.3;
                return uv + distortion * _UVDistortion;
            }

            float getRadialGradient(float2 uv) {
                float2 center = uv - 0.5;
                float dist = length(center);
                float gradient = pow(1.0 - saturate(dist * 2.0), _RadialFalloff);
                return lerp(1.0, 1.0 + gradient * _RadialGradient, 1.0);
            }

            float4 frag(v2f i) : SV_Target
            {
                // UV Distortion
                float2 distortedUV = getUVDistortion(i.texcoord);
                
                // Chromatic Aberration + UV Distortion combined
                float4 sprite = sampleWithChromaticAberration(distortedUV);
                float4 baseColor = i.color;
                
                // Multi-Layer Internal Parallax (Subtle depth)
                float2 parallaxShift = sin(_Time.y * 0.1 + float2(0, 1.5)) * _ParallaxStrength;
                float2 uvLayer1 = i.texcoord + parallaxShift;
                float2 uvLayer2 = i.texcoord - parallaxShift * 1.5;

                // Crystal Sparkles
                float3 sparkles = getCrystalSparkles(uvLayer1, _SparkleScale, 0.4, _SparkleIntensity * 2.0, _SparkleSharpness);
                float3 inclusions = getCrystalSparkles(uvLayer2, _InclusionScale, 0.15, _InclusionIntensity * 2.0, _SparkleSharpness * 0.6);
                
                // Procedural Border
                float2 uvEdge = abs(i.texcoord - 0.5) * 2.0;
                float edgeDist = max(uvEdge.x, uvEdge.y);
                float border = smoothstep(1.0 - _BorderWidth, 1.0, edgeDist);
                float3 borderColor = _BorderColor.rgb * _BorderGlow;
                
                // Fresnel Effect (edge glow)
                float2 centerDist = abs(i.texcoord - 0.5);
                float fresnel = pow(max(max(centerDist.x, centerDist.y), 0.0), _FresnelPower);
                float3 fresnelGlow = fresnel * _FresnelIntensity * _BorderColor.rgb;
                
                // Radial Light Gradient
                float radialLight = getRadialGradient(i.texcoord);
                
                // Combine
                float3 finalRGB = baseColor.rgb * radialLight;
                finalRGB += sparkles * float3(0.8, 0.9, 1.0) * 0.5;
                finalRGB += inclusions * float3(0.4, 0.6, 1.0) * 0.3;
                finalRGB += fresnelGlow * border;
                
                finalRGB = lerp(finalRGB, borderColor, border);
                
                // Edge glow
                float edge = 1.0 - sprite.a;
                finalRGB += edge * _EdgeGlowIntensity;

                return float4(finalRGB, baseColor.a * sprite.a);
            }
ENDHLSL
        }
    }
}