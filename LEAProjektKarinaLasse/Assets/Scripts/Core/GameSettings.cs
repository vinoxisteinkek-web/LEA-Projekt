using System;
using UnityEngine;

/// <summary>
/// Zentrale, szenenübergreifende Einstellungen.
/// Werte liegen statisch im Speicher (bleiben beim Szenenwechsel erhalten)
/// und werden zusätzlich in PlayerPrefs gespeichert (bleiben nach Spielneustart erhalten).
/// </summary>
public static class GameSettings
{
    private const string SensitivityKey = "Settings_MouseSensitivity";
    private const string VolumeKey = "Settings_MasterVolume";

    public const float DefaultSensitivity = 200f;
    public const float DefaultVolume = 1f;

    private static float sensitivity = DefaultSensitivity;
    private static float volume = DefaultVolume;

    /// <summary>Wird ausgelöst, wenn sich eine Einstellung ändert.</summary>
    public static event Action OnSettingsChanged;

    public static float Sensitivity
    {
        get => sensitivity;
        set
        {
            sensitivity = value;
            PlayerPrefs.SetFloat(SensitivityKey, value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
    }

    public static float Volume
    {
        get => volume;
        set
        {
            volume = Mathf.Clamp01(value);
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
    }

    // Läuft automatisch einmal beim Spielstart, bevor die erste Szene geladen wird.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load()
    {
        sensitivity = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
        volume = PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
        AudioListener.volume = volume;
    }

    public static void ResetToDefaults()
    {
        Sensitivity = DefaultSensitivity;
        Volume = DefaultVolume;
    }
}
