// Desfoque da imagem da câmera para o vidro da interface (usado pelo GlassBlurFeature).
// Passe 0 reduz a resolução com média de 4 amostras; passes 1 e 2 são uma gaussiana separável
// (horizontal e vertical) com amostragem bilinear: 4 leituras de cada lado equivalem a 13 taps.
Shader "Hidden/MontAR/GlassBlur"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _MontAR_SourceTexel; // 1/largura, 1/altura da imagem da câmera
        float4 _MontAR_BlurTexel;   // 1/largura, 1/altura da textura reduzida
        float _MontAR_BlurSpread;   // afastamento entre as amostras, em texels da textura reduzida

        half3 Gaussian(float2 uv, float2 direction)
        {
            const float offsets[4] = { 0.0, 1.4117647, 3.2941176, 5.1764706 };
            const float weights[4] = { 0.1964825, 0.2969069, 0.0944703, 0.0103813 };
            half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb * weights[0];
            [unroll]
            for (int i = 1; i < 4; i++)
            {
                float2 offset = direction * offsets[i] * _MontAR_BlurSpread;
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offset).rgb * weights[i];
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offset).rgb * weights[i];
            }
            return color;
        }
        ENDHLSL

        Pass
        {
            Name "Reduzir"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 d = _MontAR_SourceTexel.xy;
                half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x, -d.y)).rgb;
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(d.x, -d.y)).rgb;
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x, d.y)).rgb;
                color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(d.x, d.y)).rgb;
                return half4(color * 0.25, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Desfoque horizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(Gaussian(input.texcoord, float2(_MontAR_BlurTexel.x, 0.0)), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Desfoque vertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(Gaussian(input.texcoord, float2(0.0, _MontAR_BlurTexel.y)), 1.0);
            }
            ENDHLSL
        }
    }
}
