using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// =====================================
// 16MM-FILM-EFFEKT (HDRP Custom Post Process)
// =====================================
// Wird wie Bloom, Vignette usw. in einem Volume Profile hinzugefuegt:
// Add Override > Post-processing > Custom > Film Effect (16mm)
// Shader: Assets/Shader/Film/FilmEffect.shader
// Benutzt in Story 2 (Profil: Assets/Settings/VisualStyles/Film16mm_CameraFilter.asset)
[Serializable, VolumeComponentMenu("Post-processing/Custom/Film Effect (16mm)")]
public sealed class FilmEffect : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Gesamtstaerke des Effekts (0 = aus)")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("Staerke des Filmkorns")]
    public ClampedFloatParameter grain = new ClampedFloatParameter(0.6f, 0f, 2f);

    [Tooltip("Groesse der Kornklumpen in Pixeln")]
    public ClampedFloatParameter grainSize = new ClampedFloatParameter(1.6f, 0.5f, 4f);

    [Tooltip("Bild ruckelt leicht im Projektor")]
    public ClampedFloatParameter gateWeave = new ClampedFloatParameter(1f, 0f, 3f);

    [Tooltip("Helligkeitsflackern des Projektors")]
    public ClampedFloatParameter flicker = new ClampedFloatParameter(1f, 0f, 3f);

    [Tooltip("Staub und Fussel auf dem Film")]
    public ClampedFloatParameter dust = new ClampedFloatParameter(1f, 0f, 4f);

    [Tooltip("Feine senkrechte Kratzer")]
    public ClampedFloatParameter scratches = new ClampedFloatParameter(1f, 0f, 3f);

    [Tooltip("Warmer, gelblicher Filmton")]
    public ClampedFloatParameter warmth = new ClampedFloatParameter(1f, 0f, 2f);

    [Tooltip("Angehobenes Schwarz (verblasster Print)")]
    public ClampedFloatParameter blackLift = new ClampedFloatParameter(0.05f, 0f, 0.2f);

    [Tooltip("Leichte Unschaerfe des 16mm-Materials")]
    public ClampedFloatParameter softness = new ClampedFloatParameter(0.5f, 0f, 1f);

    [Tooltip("Bildrate des Films (Korn/Staub springen pro Bild)")]
    public ClampedFloatParameter frameRate = new ClampedFloatParameter(18f, 6f, 30f);

    private Material material;

    private const string ShaderName = "Hidden/Shader/FilmEffect";

    public bool IsActive() => material != null && intensity.value > 0f;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader != null)
            material = new Material(shader);
        else
            Debug.LogError("FilmEffect: Shader '" + ShaderName + "' nicht gefunden.");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (material == null)
            return;

        // Nur im Spiel anzeigen, nicht im Szenenfenster / in Vorschaubildern
        bool isGameCamera = camera.camera.cameraType == CameraType.Game;
        material.SetFloat("_Intensity", isGameCamera ? intensity.value : 0f);
        material.SetFloat("_Grain", grain.value);
        material.SetFloat("_GrainSize", grainSize.value);
        material.SetFloat("_GateWeave", gateWeave.value);
        material.SetFloat("_Flicker", flicker.value);
        material.SetFloat("_Dust", dust.value);
        material.SetFloat("_Scratches", scratches.value);
        material.SetFloat("_Warmth", warmth.value);
        material.SetFloat("_BlackLift", blackLift.value);
        material.SetFloat("_Softness", softness.value);
        material.SetFloat("_FrameRate", frameRate.value);
        material.SetFloat("_FilmTime", Time.unscaledTime);
        material.SetTexture("_MainTex", source);

        HDUtils.DrawFullScreen(cmd, material, destination, shaderPassId: 0);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(material);
    }
}
