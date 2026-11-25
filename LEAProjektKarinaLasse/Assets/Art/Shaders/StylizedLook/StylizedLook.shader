Shader "Hidden/Shader/StylizedLook"
{
    Properties
    {
        _MainTex("Main Texture", 2DArray) = "grey" {}
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    TEXTURE2D_X(_MainTex);

    float _Intensity;
    float _PixelHeight;      // 0 = aus, sonst vertikale Aufloesung (z.B. 240)
    float _ColorSteps;       // 0 = aus, sonst Farbstufen pro Kanal
    float _Dither;           // Bayer-Dithering
    float _Halation;         // rotes Glimmen um helle Lichter (Filmmaterial)
    float _HalationThreshold;
    float _HalationRadius;
    float4 _HalationTint;
    float _Grain;
    float _GrainSize;
    float _FrameRate;
    float _StyleTime;

    static const float BayerM[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

    float Hash21(float2 p)
    {
        float3 p3 = frac(float3(p.xyx) * 0.1031);
        p3 += dot(p3, p3.yzx + 33.33);
        return frac((p3.x + p3.y) * p3.z);
    }

    float ValueNoise(float2 p)
    {
        float2 i = floor(p);
        float2 f = frac(p);
        f = f * f * (3.0 - 2.0 * f);
        float a = Hash21(i);
        float b = Hash21(i + float2(1, 0));
        float c = Hash21(i + float2(0, 1));
        float d = Hash21(i + float2(1, 1));
        return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
    }

    float3 SampleLinear(float2 uv)
    {
        uv = saturate(uv);
        return SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(uv)).rgb;
    }

    float3 SamplePoint(float2 uv)
    {
        uv = saturate(uv);
        return SAMPLE_TEXTURE2D_X(_MainTex, s_point_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(uv)).rgb;
    }

    float Luma(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = input.texcoord;
        float aspect = _ScreenSize.x / max(_ScreenSize.y, 1.0);
        float frame = floor(_StyleTime * max(_FrameRate, 1.0));

        float3 original = SampleLinear(uv);

        // 1) Pixel-Aufloesung reduzieren (PS1-/Retro-Look)
        float2 cells = _ScreenSize.xy;
        float2 suv = uv;
        bool pixelate = _PixelHeight > 1.0;
        if (pixelate)
        {
            cells = float2(floor(_PixelHeight * aspect), _PixelHeight);
            suv = (floor(uv * cells) + 0.5) / cells;
        }
        float3 col = pixelate ? SamplePoint(suv) : SampleLinear(suv);

        // 2) Halation: roter/oranger Schein um helle Lichtquellen (wie bei echtem Kinofilm)
        if (_Halation > 0.001)
        {
            float3 glow = 0;
            float2 px = _ScreenSize.zw * _HalationRadius;
            [unroll] for (int k = 0; k < 12; k++)
            {
                float a = k * 0.5235988;            // 30 Grad Schritte
                float r = (k % 2 == 0) ? 1.0 : 0.55;
                float3 s = SampleLinear(suv + float2(cos(a), sin(a)) * px * r);
                glow += max(s - _HalationThreshold, 0.0);
            }
            glow /= 12.0;
            float g = Luma(glow);
            col += _HalationTint.rgb * g * _Halation;
        }

        // ab hier im Gamma-Raum
        col = LinearToSRGB(saturate(col));

        // 3) Filmkorn
        if (_Grain > 0.001)
        {
            float2 gp = uv * _ScreenSize.xy / max(_GrainSize, 0.5);
            float2 go = float2(Hash21(float2(frame, 1.3)), Hash21(float2(2.7, frame))) * 512.0;
            float n = ValueNoise(gp + go) * 0.6 + Hash21(floor(gp * 1.7) + go) * 0.4 - 0.5;
            float l = Luma(col);
            float mask = lerp(1.0, 0.4, smoothstep(0.35, 1.0, l));
            col += n * 0.14 * _Grain * mask;
        }

        // 4) Farbstufen + Dithering (begrenzte Farbtiefe)
        if (_ColorSteps > 1.0)
        {
            uint2 p = (uint2)(pixelate ? floor(uv * cells) : floor(uv * _ScreenSize.xy));
            float b = (BayerM[(p.y & 3) * 4 + (p.x & 3)] + 0.5) / 16.0 - 0.5;
            col = floor(col * (_ColorSteps - 1.0) + 0.5 + b * _Dither) / (_ColorSteps - 1.0);
        }

        col = SRGBToLinear(saturate(col));
        return float4(lerp(original, col, _Intensity), 1);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "StylizedLook"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
