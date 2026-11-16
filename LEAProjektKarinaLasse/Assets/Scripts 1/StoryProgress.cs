using UnityEngine;

// =====================================
// STORY-FORTSCHRITT (gespeichert in PlayerPrefs)
// =====================================
// Merkt sich dauerhaft (auch nach Spielneustart), welche Storys
// schon durchgespielt wurden.
public static class StoryProgress
{
    private const string Story1Key = "Story1Completed";

    public static bool IsStory1Completed()
    {
        return PlayerPrefs.GetInt(Story1Key, 0) == 1;
    }

    public static void CompleteStory1()
    {
        PlayerPrefs.SetInt(Story1Key, 1);
        PlayerPrefs.Save();
    }

    // Zum Testen: Fortschritt zuruecksetzen
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(Story1Key);
        PlayerPrefs.Save();
    }
}
