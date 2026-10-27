// Vidro fosco para a interface (uGUI): o fundo desfocado (_MontAR_GlassBlur, publicado pelo
// GlassBlurFeature) aparece atrás do cartão, com a cor do Image por cima na força _TintStrength.
// O sprite só dá o formato (cantos arredondados). O alfa do Image (e do CanvasGroup) é a opacidade
// do cartão inteiro, então o fade das telas continua funcionando.
Shader "MontAR/UI Glass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Formato (sprite)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TintStrength ("Força da cor sobre o fundo", Range(0, 1)) = 0.55
        _Saturation ("Saturação do fundo", Range(0, 2)) = 1.35
        _Brightness ("Brilho do fundo", Range(0, 2)) = 0.95

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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            Name "Vidro"
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
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _MontAR_GlassBlur;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            half _TintStrength;
            half _Saturation;
            half _Brightness;
            int _UIVertexColorAlwaysGammaSpace;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                // Mesma regra do shader padrão da UI: a cor do vértice vem em gama.
                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                    v.color.rgb = UIGammaToLinear(v.color.rgb);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half shape = tex2D(_MainTex, IN.texcoord).a + _TextureSampleAdd.a;
                half3 backdrop = tex2D(_MontAR_GlassBlur, IN.screenPos.xy / IN.screenPos.w).rgb;
                half luminance = dot(backdrop, half3(0.2126, 0.7152, 0.0722));
                backdrop = lerp(luminance.xxx, backdrop, _Saturation) * _Brightness;

                half4 color = half4(lerp(backdrop, IN.color.rgb, _TintStrength), shape * IN.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
