Shader "UI/VictoryScreen/VictoryPanelGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.2, 0.15, 0.05, 0.6) // Golden/Bronze Tint
        
        _ScanlineSpeed ("Scanline Speed", Float) = 0.5
        _ScanlineDensity ("Scanline Density", Float) = 15.0

        [Header(Crystal Details)]
        _ParallaxStrength ("Internal Parallax", Range(0, 0.1)) = 0.03
        _ShimmerIntensity ("Caustic Shimmer", Range(0, 5)) = 2.0
        _ShimmerSpeed ("Shimmer Speed", Float) = 1.2
        
        [Header(Border Settings)]
        _BorderColor ("Border Color", Color) = (1.0, 0.9, 0.5, 1.0) // Gold Border
        _BorderWidth ("Border Width", Range(0, 0.5)) = 0.02
        _BorderGlow ("Border Glow", Float) = 3.0
        
        _CenterAlphaScale ("Center Alpha Scale", Range(0, 1)) = 0.3 // How transparent the center is
        _VignetteSoftness ("Vignette Softness", Range(0.1, 2)) = 0.8
        
        _ShineSpeed ("Shine Speed", Float) = 2.0
        _ShineWidth ("Shine Width", Range(0.01, 0.5)) = 0.1
        _ShineColor ("Shine Color", Color) = (1, 1, 1, 1)

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _Color;
            float _ParallaxStrength;
            float4 _BorderColor;
            float _BorderWidth;
            float _BorderGlow;
            float _CenterAlphaScale;
            float _VignetteSoftness;
            float _ShineSpeed;
            float _ShineWidth;
            float4 _ShineColor;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float3 getSparkles(float2 uv, float scale, float speed) {
                float2 sUV = uv * scale;
                float n = hash(floor(sUV) + floor(_Time.y * speed));
                return pow(n, 50.0) * 2.0;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 sprite = tex2D(_MainTex, i.texcoord);
                float4 baseColor = i.color;
                
                // Multi-Layer Internal Parallax
                float2 parallaxShift = sin(_Time.y * 0.08 + float2(0, 1.2)) * _ParallaxStrength;
                float2 uvLayer1 = i.texcoord + parallaxShift;
                float2 uvLayer2 = i.texcoord - parallaxShift * 2.0;

                // Internal Sparkles
                float3 sparkles = getSparkles(uvLayer1, 15.0, 0.4);
                float3 inclusions = getSparkles(uvLayer2, 25.0, 0.15) * 0.3;
                
                // Vignette Alpha
                float2 distFromCenter = i.texcoord - 0.5;
                float dist = length(distFromCenter);
                float vignette = smoothstep(0.0, _VignetteSoftness, dist);
                float finalAlpha = baseColor.a * lerp(_CenterAlphaScale, 1.0, vignette);
                
                // Victory Sweep Shine
                float sweepPos = frac(_Time.y * _ShineSpeed * 0.05) * 3.0 - 1.0;
                float victoryShine = smoothstep(sweepPos - _ShineWidth, sweepPos, i.texcoord.x + i.texcoord.y) - 
                             smoothstep(sweepPos, sweepPos + _ShineWidth, i.texcoord.x + i.texcoord.y);
                
                // Procedural Border
                float2 uvEdge = abs(i.texcoord - 0.5) * 2.0;
                float edgeVal = max(uvEdge.x, uvEdge.y);
                float border = smoothstep(1.0 - _BorderWidth, 1.0, edgeVal);
                
                // Clean Border
                float3 borderColor = _BorderColor.rgb * _BorderGlow;
                
                // Combine
                float3 finalRGB = baseColor.rgb;
                finalRGB += sparkles * float3(1.0, 0.9, 0.7) * 0.6; 
                finalRGB += inclusions * float3(1.0, 0.8, 0.5) * 0.3;
                finalRGB += victoryShine * _ShineColor.rgb * 0.4;
                
                finalRGB = lerp(finalRGB, borderColor, border);

                return float4(finalRGB, finalAlpha * sprite.a);
            }
ENDHLSL
        }
    }
}
