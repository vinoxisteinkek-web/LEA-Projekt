Shader "Hidden/Shader/FilmEffect"
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
    float _Grain;
    float _GrainSize;
    float _GateWeave;
    float _Flicker;
    float _Dust;
    float _Scratches;
    float _Warmth;
    float _BlackLift;
    float _Softness;
    float _FrameRate;
    float _FilmTime;

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

    float2 Hash22(float2 p)
    {
        float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
        p3 += dot(p3, p3.yzx + 33.33);
        return frac((p3.xx + p3.yz) * p3.zy);
    }

    // weiches Wertrauschen fuer klumpiges Filmkorn
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

    float3 SampleSource(float2 uv)
    {
        uv = saturate(uv);
        float3 c = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(uv)).rgb;
        // Film-Look im Gamma-Raum berechnen
        return LinearToSRGB(saturate(c));
    }

    float Luma(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

    // ---------- Effekt ----------

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = input.texcoord;
        float t = _FilmTime;
        // Film laeuft mit eigener Bildrate (Korn, Staub, Flackern springen pro Bild)
        float frame = floor(t * max(_FrameRate, 1.0));
        float aspect = _ScreenSize.x / max(_ScreenSize.y, 1.0);

        float3 original = SampleSource(uv);

        // 1) Gate Weave: Bild ruckelt leicht im Projektor (pro Bild + langsames Driften)
        float2 weave;
        weave.x = (Hash11(frame * 1.31) - 0.5) * 0.0012 + sin(t * 0.9) * 0.0006;
        weave.y = (Hash11(frame * 2.17 + 4.0) - 0.5) * 0.0018 + sin(t * 0.63 + 1.7) * 0.0009;
        float2 suv = uv + weave * _GateWeave;

        // 2) Leichte Weichheit des 16mm-Materials (kleiner Blur)
        float2 px = _ScreenSize.zw * (1.0 + _Softness);
        float3 col = SampleSource(suv) * 0.5;
        col += SampleSource(suv + float2(px.x, 0)) * 0.125;
        col += SampleSource(suv - float2(px.x, 0)) * 0.125;
        col += SampleSource(suv + float2(0, px.y)) * 0.125;
        col += SampleSource(suv - float2(0, px.y)) * 0.125;
        col = lerp(SampleSource(suv), col, saturate(_Softness));

        // 3) Farbe: warm/gelblich, Lichter leicht cremig, Schatten etwas braun-gruen
        float l = Luma(col);
        float3 warmHi = float3(1.06, 1.0, 0.86);
        float3 warmLo = float3(1.02, 1.0, 0.92);
        col *= lerp(float3(1, 1, 1), lerp(warmLo, warmHi, l), _Warmth);
        // leicht entsaettigt wie altes Filmmaterial
        col = lerp(l.xxx, col, 1.0 - 0.15 * _Warmth);

        // 4) Angehobenes Schwarz (Print-Look) mit warmem Ton
        float3 liftTint = float3(1.0, 0.84, 0.62);
        col = col * (1.0 - _BlackLift) + liftTint * _BlackLift;

        // 5) Flackern des Projektors
        float flick = (Hash11(frame * 0.71) - 0.5) * 0.06 + sin(t * 23.0) * 0.008;
        col *= 1.0 + flick * _Flicker;

        // 6) Filmkorn: klumpig, staerker in Mitten/Schatten
        float2 gp = uv * _ScreenSize.xy / max(_GrainSize, 0.5);
        float2 go = Hash22(float2(frame, frame * 1.7)) * 512.0;
        float g = ValueNoise(gp + go) * 0.6 + Hash21(floor(gp * 1.7) + go) * 0.4;
        g = g - 0.5;
        float grainMask = lerp(1.0, 0.45, smoothstep(0.35, 1.0, l)); // Lichter weniger Korn
        col += g * 0.16 * _Grain * grainMask;

        // 7) Staub & Fussel: nur wenige Teilchen pro Bild (wie echter Projektionsdreck)
        float2 duv = float2(uv.x * aspect, uv.y);
        float darkDust = 0.0;
        float brightDust = 0.0;
        [unroll] for (int k = 0; k < 6; k++)
        {
            float2 seed = float2(frame * 7.13 + k * 31.7, frame * 3.71 + k * 11.3);
            float chance = Hash21(seed);
            if (chance < 0.4 * _Dust)
            {
                float2 c = Hash22(seed + 17.0) * float2(aspect, 1.0);
                float r = 0.0015 + Hash11(chance * 91.0 + k) * 0.004;
                float2 d = duv - c;
                float ang = Hash11(chance * 17.0 + k) * 6.2831;
                float2 dr = float2(d.x * cos(ang) - d.y * sin(ang), d.x * sin(ang) + d.y * cos(ang));
                dr.x *= lerp(1.0, 0.3, Hash11(chance * 5.0 + k));   // laenglich = Fussel
                // unregelmaessiger Rand
                float edge = r * (0.75 + 0.5 * ValueNoise(dr * 900.0 + k));
                float m = 1.0 - smoothstep(edge * 0.55, edge, length(dr));
                if (Hash11(chance * 3.0 + k) > 0.3) darkDust = max(darkDust, m);
                else brightDust = max(brightDust, m);
            }
        }
        col *= 1.0 - darkDust * 0.85;
        col += brightDust * 0.45;

        // 8) Kratzer: feine senkrechte Linien, die eine Weile bleiben und leicht wandern
        float scratch = 0.0;
        [unroll] for (int s = 0; s < 2; s++)
        {
            float life = floor(t * (0.6 + s * 0.35) + s * 3.0);   // jeder Kratzer lebt ~1-2 s
            float on = step(0.55, Hash11(life * 4.7 + s));
            float x = Hash11(life * 1.93 + s * 11.0) + sin(t * 1.3 + s) * 0.004;
            x += (Hash11(frame + s * 7.0) - 0.5) * 0.0015;             // zittert pro Bild
            float w = 0.00045 + Hash11(life + 2.0) * 0.0004;
            float sLine = 1.0 - smoothstep(w * 0.3, w, abs(uv.x - x));
            // nicht ueber die ganze Hoehe durchgehend
            float seg = smoothstep(0.2, 0.5, ValueNoise(float2(x * 50.0, uv.y * 6.0 + life * 3.0)));
            scratch += sLine * seg * on;
        }
        col += scratch * 0.18 * _Scratches;

        col = saturate(col);
        col = SRGBToLinear(col);
        original = SRGBToLinear(original);
        return float4(lerp(original, col, _Intensity), 1);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "FilmEffect"
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
