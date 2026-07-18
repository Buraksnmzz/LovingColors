Shader "UI/SoftLightCard"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _EffectTexSecondary ("Effect Secondary Texture", 2D) = "black" {}
        _EffectTex ("Effect Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _EffectTexSecondary;
            sampler2D _EffectTex;
            fixed4 _Color;
            float4 _ClipRect;

            float SoftLightChannel(float baseValue, float blendValue)
            {
                if (blendValue <= 0.5)
                {
                    return baseValue - (1.0 - 2.0 * blendValue) * baseValue * (1.0 - baseValue);
                }
                
                float d = (baseValue <= 0.25)
                    ? ((16.0 * baseValue - 12.0) * baseValue + 4.0) * baseValue
                    : sqrt(baseValue);
                
                return baseValue + (2.0 * blendValue - 1.0) * (d - baseValue);
            }

            float3 SoftLight(float3 baseColor, float3 blendColor)
            {
                return float3(
                    SoftLightChannel(baseColor.r, blendColor.r),
                    SoftLightChannel(baseColor.g, blendColor.g),
                    SoftLightChannel(baseColor.b, blendColor.b)
                );
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 baseSample = tex2D(_MainTex, IN.texcoord);
                fixed4 effectSecondarySample = tex2D(_EffectTexSecondary, IN.texcoord);
                fixed4 materialEffectSample = tex2D(_EffectTex, IN.texcoord);
                fixed4 effectSample = effectSecondarySample.a > 0.001 ? effectSecondarySample : materialEffectSample;

                float3 tintedBaseRgb = baseSample.rgb * IN.color.rgb;
                float3 blendedRgb = SoftLight(saturate(tintedBaseRgb), saturate(effectSample.rgb));
                blendedRgb = lerp(tintedBaseRgb, blendedRgb, effectSample.a);

                float alpha = baseSample.a * IN.color.a;
                fixed4 result = fixed4(blendedRgb, alpha);

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
