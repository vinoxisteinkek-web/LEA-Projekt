using UnityEngine;

// =====================================
// FRAME-RATE EINSTELLUNGEN
// =====================================
// VSync aus + Frame-Limit auf die Hz-Zahl des Monitors.
// Mit VSync springt das Spiel bei jedem Frame, der etwas laenger als
// 16,7 ms braucht, auf 30 FPS -> fuehlt sich ruckelig an.
// Ohne VSync laeuft es in dem Fall einfach mit z. B. 55 FPS weiter.
// Laeuft automatisch beim Spielstart, muss an kein Objekt gehaengt werden.
public static class FrameRateSettings
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        QualitySettings.vSyncCount = 0;

        double hz = Screen.currentResolution.refreshRateRatio.value;
        Application.targetFrameRate = hz > 1 ? Mathf.RoundToInt((float)hz) : 60;
    }
}
