Shader "Hidden/Shader/VHSEffect"
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
    float _Wobble;
    float _ColorBleed;
    float _Noise;
    float _Scanlines;
    float _TrackingBand;
    float _BottomNoise;
    float _HorizontalRes;
    float _Saturation;
    float _VhsTime;

    // ---------- Hilfsfunktionen ----------

    float Hash11(float p)
    {
        p = frac(p * 0.1031);
        p *= p + 33.33;
        p *= p + p;
        return frac(p);
    }

    float Hash21(float2 p)
    {
        float3 p3 = frac(float3(p.xyx) * 0.1031);
        p3 += dot(p3, p3.yzx + 33.33);
        return frac((p3.x + p3.y) * p3.z);
    }

    float3 RGBToYIQ(float3 c)
    {
        return float3(
            dot(c, float3(0.299, 0.587, 0.114)),
            dot(c, float3(0.596, -0.274, -0.322)),
            dot(c, float3(0.211, -0.523, 0.312)));
    }

    float3 YIQToRGB(float3 c)
    {
        return float3(
            dot(c, float3(1.0, 0.956, 0.621)),
            dot(c, float3(1.0, -0.272, -0.647)),
            dot(c, float3(1.0, -1.106, 1.703)));
    }

    float3 SampleSource(float2 uv)
    {
        uv = saturate(uv);
        float3 c = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(uv)).rgb;
        // Videosignal arbeitet im Gamma-Raum -> Effekte dort berechnen
        return LinearToSRGB(saturate(c));
    }

    // ---------- Effekt ----------

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = input.texcoord;
        float t = _VhsTime;
        float3 original = SampleSource(uv);

        // Bildzeilen wie bei PAL/NTSC (ca. 240 sichtbare Zeilen)
        float rowIndex = floor(uv.y * 240.0);
        float frame = floor(t * 30.0);

        // 1) Zeilen-Wackeln: leichtes Grundzittern + langsame Welle
        float jitter = (Hash11(rowIndex * 1.7 + frame * 13.1) - 0.5) * 0.0012;
        float wave = sin(uv.y * 18.0 + t * 1.3) * 0.0007 + sin(uv.y * 160.0 + t * 7.0) * 0.00025;
        float xOffset = (jitter + wave) * _Wobble;

        // 2) Tracking-Band, das langsam von oben nach unten laeuft
        float bandPos = 1.0 - frac(t * 0.07); // uv.y = 1 ist oben -> Band laeuft nach unten
        float bandDist = abs(uv.y - bandPos);
        float band = saturate(1.0 - bandDist / 0.035) * _TrackingBand;
        xOffset += band * (Hash11(rowIndex + frame * 7.3) - 0.5) * 0.02;

        // 3) Stoerstreifen am unteren Bildrand (Kopfumschaltung)
        float bottom = saturate((0.045 - uv.y) / 0.045) * _BottomNoise; // unterer Bildrand
        xOffset += bottom * (0.015 + (Hash11(rowIndex * 3.1 + frame) - 0.5) * 0.03);

        // 4) Geringere horizontale Aufloesung: Helligkeit mit 3 Abtastungen weichzeichnen
        float px = 1.0 / max(_HorizontalRes, 1.0);
        float2 luv = float2(uv.x + xOffset, uv.y);
        float y =
            RGBToYIQ(SampleSource(luv - float2(px, 0))).x * 0.25 +
            RGBToYIQ(SampleSource(luv)).x * 0.5 +
            RGBToYIQ(SampleSource(luv + float2(px, 0))).x * 0.25;

        // 5) Farbe: noch unschaerfer und leicht nach rechts verschoben (Color Bleed)
        float2 iq = 0;
        float cpx = px * 2.5 * _ColorBleed;
        float2 cuv = luv - float2(0.0025 * _ColorBleed, 0);
        [unroll] for (int i = -3; i <= 3; i++)
        {
            iq += RGBToYIQ(SampleSource(cuv + float2(cpx * i, 0))).yz;
        }
        iq /= 7.0;
        iq *= _Saturation;

        float3 col = YIQToRGB(float3(y, iq));

        // 6) Leicht angehobenes Schwarz, etwas weniger Kontrast
        col = col * 0.93 + 0.025;

        // 7) Bildzeilen
        float sl = sin(uv.y * _ScreenSize.y * 0.5 * PI);
        col *= 1.0 - _Scanlines * 0.18 * (sl * sl);

        // 8) Rauschen / Schnee (staerker im Tracking-Band und unten)
        float n = Hash21(floor(uv * _ScreenSize.xy * 0.5) + frame * 17.0);
        float snow = (n - 0.5) * 0.08 * _Noise;
        float streak = step(0.985 - band * 0.2 - bottom * 0.3, Hash21(float2(rowIndex, frame))) * (0.25 + band * 0.4);
        col += snow + streak * Hash21(uv * 91.7 + frame) * _Noise;

        // 9) Ganz leichtes Flackern der Helligkeit
        col *= 1.0 + (Hash11(frame * 0.37) - 0.5) * 0.03;

        // Im Bandbereich etwas aufhellen/ausbleichen
        col = lerp(col, col * 1.15 + 0.03, band * 0.5);

        col = SRGBToLinear(saturate(col));
        original = SRGBToLinear(original);
        return float4(lerp(original, col, _Intensity), 1);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "VHSEffect"
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
