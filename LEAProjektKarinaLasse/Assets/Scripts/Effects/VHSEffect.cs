using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// =====================================
// VHS-EFFEKT (HDRP Custom Post Process)
// =====================================
// Wird wie Bloom, Vignette usw. in einem Volume Profile hinzugefuegt:
// Add Override > Post-processing > Custom > VHS Effect
// Shader: Assets/Shader/VHS/VHSEffect.shader
[Serializable, VolumeComponentMenu("Post-processing/Custom/VHS Effect")]
public sealed class VHSEffect : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Gesamtstaerke des Effekts (0 = aus)")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("Zeilen wackeln seitlich (schlechtes Band)")]
    public ClampedFloatParameter wobble = new ClampedFloatParameter(1f, 0f, 3f);

    [Tooltip("Farben laufen seitlich aus und sind gegenueber der Helligkeit verschoben")]
    public ClampedFloatParameter colorBleed = new ClampedFloatParameter(1f, 0f, 3f);

    [Tooltip("Rauschen / Schnee im Bild")]
    public ClampedFloatParameter noise = new ClampedFloatParameter(0.5f, 0f, 2f);

    [Tooltip("Sichtbare Bildzeilen")]
    public ClampedFloatParameter scanlines = new ClampedFloatParameter(0.4f, 0f, 1f);

    [Tooltip("Gestoerter Streifen, der langsam durchs Bild laeuft")]
    public ClampedFloatParameter trackingBand = new ClampedFloatParameter(1f, 0f, 2f);

    [Tooltip("Stoerstreifen am unteren Bildrand (Kopfumschaltung)")]
    public ClampedFloatParameter bottomNoise = new ClampedFloatParameter(1f, 0f, 2f);

    [Tooltip("Horizontale Aufloesung des Bandes (weniger = unschaerfer)")]
    public ClampedFloatParameter horizontalResolution = new ClampedFloatParameter(420f, 120f, 1920f);

    [Tooltip("Farbsaettigung des Bandes")]
    public ClampedFloatParameter saturation = new ClampedFloatParameter(0.8f, 0f, 1.5f);

    private Material material;

    private const string ShaderName = "Hidden/Shader/VHSEffect";

    public bool IsActive() => material != null && intensity.value > 0f;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader != null)
            material = new Material(shader);
        else
            Debug.LogError("VHSEffect: Shader '" + ShaderName + "' nicht gefunden.");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (material == null)
            return;

        // Nur im Spiel anzeigen, nicht im Szenenfenster / in Vorschaubildern
        bool isGameCamera = camera.camera.cameraType == CameraType.Game;
        material.SetFloat("_Intensity", isGameCamera ? intensity.value : 0f);
        material.SetFloat("_Wobble", wobble.value);
        material.SetFloat("_ColorBleed", colorBleed.value);
        material.SetFloat("_Noise", noise.value);
        material.SetFloat("_Scanlines", scanlines.value);
        material.SetFloat("_TrackingBand", trackingBand.value);
        material.SetFloat("_BottomNoise", bottomNoise.value);
        material.SetFloat("_HorizontalRes", horizontalResolution.value);
        material.SetFloat("_Saturation", saturation.value);
        // Laeuft auch in der Pause weiter (wie ein angehaltenes Band)
        material.SetFloat("_VhsTime", Time.unscaledTime);
        material.SetTexture("_MainTex", source);

        HDUtils.DrawFullScreen(cmd, material, destination, shaderPassId: 0);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(material);
    }
}
