using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// =====================================
// STYLIZED LOOK (HDRP Custom Post Process)
// =====================================
// Baukasten fuer verschiedene Kamerafilter:
// Pixel-Aufloesung, Farbstufen + Dithering, Halation (Glimmen um Lichter), Filmkorn.
// Add Override > Post-processing > Custom > Stylized Look
// Shader: Assets/Art/Shaders/StylizedLook/StylizedLook.shader
// Benutzt von den Story-2-Filtervarianten in Assets/Settings/VisualStyles/Story2/
[Serializable, VolumeComponentMenu("Post-processing/Custom/Stylized Look")]
public sealed class StylizedLook : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Gesamtstaerke des Effekts (0 = aus)")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Header("Retro-Aufloesung")]
    [Tooltip("Vertikale Pixel-Aufloesung (0 = aus, 240 = PS1-artig)")]
    public ClampedFloatParameter pixelHeight = new ClampedFloatParameter(0f, 0f, 1080f);

    [Tooltip("Farbstufen pro Kanal (0 = aus, 32 = PS1-artig)")]
    public ClampedFloatParameter colorSteps = new ClampedFloatParameter(0f, 0f, 64f);

    [Tooltip("Dithering-Muster bei reduzierten Farbstufen")]
    public ClampedFloatParameter dither = new ClampedFloatParameter(1f, 0f, 2f);

    [Header("Halation (Kinofilm)")]
    [Tooltip("Roter/oranger Schein um helle Lichtquellen")]
    public ClampedFloatParameter halation = new ClampedFloatParameter(0f, 0f, 4f);

    [Tooltip("Ab welcher Helligkeit Lichter glimmen")]
    public ClampedFloatParameter halationThreshold = new ClampedFloatParameter(0.8f, 0f, 4f);

    [Tooltip("Radius des Scheins in Pixeln")]
    public ClampedFloatParameter halationRadius = new ClampedFloatParameter(8f, 1f, 32f);

    public ColorParameter halationTint = new ColorParameter(new Color(1f, 0.25f, 0.08f), false, false, true);

    [Header("Korn")]
    public ClampedFloatParameter grain = new ClampedFloatParameter(0f, 0f, 2f);
    public ClampedFloatParameter grainSize = new ClampedFloatParameter(1.2f, 0.5f, 4f);
    [Tooltip("Wie oft das Korn pro Sekunde springt")]
    public ClampedFloatParameter frameRate = new ClampedFloatParameter(24f, 6f, 60f);

    private Material material;
    private const string ShaderName = "Hidden/Shader/StylizedLook";

    public bool IsActive() => material != null && intensity.value > 0f;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader != null)
            material = new Material(shader);
        else
            Debug.LogError("StylizedLook: Shader '" + ShaderName + "' nicht gefunden.");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (material == null)
            return;

        // Nur im Spiel anzeigen, nicht im Szenenfenster / in Vorschaubildern
        bool isGameCamera = camera.camera.cameraType == CameraType.Game;
        material.SetFloat("_Intensity", isGameCamera ? intensity.value : 0f);
        material.SetFloat("_PixelHeight", pixelHeight.value);
        material.SetFloat("_ColorSteps", colorSteps.value);
        material.SetFloat("_Dither", dither.value);
        material.SetFloat("_Halation", halation.value);
        material.SetFloat("_HalationThreshold", halationThreshold.value);
        material.SetFloat("_HalationRadius", halationRadius.value);
        material.SetColor("_HalationTint", halationTint.value);
        material.SetFloat("_Grain", grain.value);
        material.SetFloat("_GrainSize", grainSize.value);
        material.SetFloat("_FrameRate", frameRate.value);
        material.SetFloat("_StyleTime", Time.unscaledTime);
        material.SetTexture("_MainTex", source);

        HDUtils.DrawFullScreen(cmd, material, destination, shaderPassId: 0);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(material);
    }
}
