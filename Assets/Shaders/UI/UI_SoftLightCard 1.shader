Shader "UI/SoftLightCard_Bright"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BaseTex ("Main Texture (Card)", 2D) = "white" {}
        _EffectTex ("Effect Texture (Shadow)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Doğrudan ana rengin parlaklığını ve canlılığını artıracak yeni ayarlar:
        _MainBrightness ("Main Color Brightness", Range(1.0, 3.0)) = 1.4
        _MainSaturation ("Main Color Saturation", Range(1.0, 2.0)) = 1.2

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
            sampler2D _BaseTex;
            sampler2D _EffectTex;
            fixed4 _Color;
            float4 _ClipRect;
            float _MainBrightness;
            float _MainSaturation;

            // RGB -> HSV Dönüşümü (Renk manipülasyonu için)
            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            // HSV -> RGB Dönüşümü
            float3 HSVtoRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            // Orijinal Adobe/W3C Soft Light Formülü
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
                fixed4 spriteSample = tex2D(_MainTex, IN.texcoord);
                fixed4 baseSample = tex2D(_BaseTex, IN.texcoord);
                fixed4 effectSample = tex2D(_EffectTex, IN.texcoord);

                // 1. Orijinal rengi ata
                float3 tintedBaseRgb = baseSample.rgb * IN.color.rgb;

                // 2. Sadece ana rengi parlat ve canlandır (HSV üzerinden)
                float3 hsv = RGBtoHSV(tintedBaseRgb);
                hsv.y *= _MainSaturation; // Canlılık / Doygunluk ayarı
                hsv.z *= _MainBrightness; // Saf parlaklık ayarı
                float3 brightBaseRgb = saturate(HSVtoRGB(hsv));

                // 3. Parlatılmış rengin üzerine Soft Light gölgesini bindir
                float3 blendedRgb = SoftLight(brightBaseRgb, saturate(effectSample.rgb));
                
                // Efekt görselinin şeffaf yerlerinde parlatılmış orijinal rengi koru
                blendedRgb = lerp(brightBaseRgb, blendedRgb, effectSample.a);

                // Şeffaflık hesabı
                float alpha = baseSample.a * spriteSample.a * IN.color.a;
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