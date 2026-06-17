Shader "UI/HUD/RefractiveGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        
        [Header(Border Settings)]
        _BorderColor ("Border Color", Color) = (0.4, 0.7, 1.0, 1.0)
        _BorderWidth ("Border Width", Range(0, 0.5)) = 0.02
        _BorderGlow ("Border Glow", Float) = 2.0
        _FresnelPower ("Glow Falloff (Fresnel)", Range(0.1, 5.0)) = 2.0
        _FresnelIntensity ("Glow Intensity", Range(0, 2.0)) = 0.3
        
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _Color;
            float4 _BorderColor;
            float _BorderWidth;
            float _BorderGlow;
            float _FresnelPower;
            float _FresnelIntensity;

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

            float4 frag(v2f i) : SV_Target
            {
                // Sample sprite texture for UI masking/rounded corners
                float4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
                float4 baseColor = i.color;
                
                // Procedural border calculation
                float2 uvEdge = abs(i.texcoord - 0.5) * 2.0;
                float edgeDist = max(uvEdge.x, uvEdge.y);
                float border = smoothstep(1.0 - _BorderWidth, 1.0, edgeDist);
                
                // Base glowing border color
                float3 finalRGB = _BorderColor.rgb * _BorderGlow;
                
                // Fresnel inner glow effect on the border
                float2 centerDist = abs(i.texcoord - 0.5);
                float fresnel = pow(max(max(centerDist.x, centerDist.y), 0.0), _FresnelPower);
                finalRGB += fresnel * _FresnelIntensity * _BorderColor.rgb;
                
                // Apply UI Tint/Vertex color
                finalRGB *= baseColor.rgb;

                // Mask the alpha so the center is invisible, fading up into the border
                float finalAlpha = baseColor.a * sprite.a * border * _BorderColor.a;

                return float4(finalRGB, finalAlpha);
            }
        ENDHLSL
        }
    }
}