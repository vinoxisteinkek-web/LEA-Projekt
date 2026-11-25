using UnityEngine;

// =====================================
// STORY-FORTSCHRITT (gespeichert in PlayerPrefs)
// =====================================
// Merkt sich dauerhaft (auch nach Spielneustart), welche Storys
// schon durchgespielt wurden.
public static class StoryProgress
{
    private const string Story1Key = "Story1Completed";
    private const string Story2Key = "Story2Completed";

    public static bool IsStory1Completed()
    {
        return PlayerPrefs.GetInt(Story1Key, 0) == 1;
    }

    public static void CompleteStory1()
    {
        PlayerPrefs.SetInt(Story1Key, 1);
        PlayerPrefs.Save();
    }

    // Story 2 (Tankstelle + Watchtower) durchgespielt -> Story 3 kann freigeschaltet werden
    public static bool IsStory2Completed()
    {
        return PlayerPrefs.GetInt(Story2Key, 0) == 1;
    }

    public static void CompleteStory2()
    {
        PlayerPrefs.SetInt(Story2Key, 1);
        PlayerPrefs.Save();
    }

    // Zum Testen: Fortschritt zuruecksetzen
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(Story1Key);
        PlayerPrefs.DeleteKey(Story2Key);
        PlayerPrefs.Save();
    }
}
