using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================
// HAUPTMENUE - STORY AUSWAHL
// =====================================
// "Play" oeffnet die Story-Auswahl.
// Story 2 ist gesperrt, bis Story 1 einmal durchgespielt wurde
// (egal welches Ending).
public class MainMenuStorySelect : MonoBehaviour
{
    [Header("Hauptmenue (wird beim Oeffnen der Auswahl ausgeblendet)")]
    [SerializeField] private GameObject[] mainMenuObjects;

    [Header("Story Auswahl")]
    [SerializeField] private GameObject storySelectPanel;
    [SerializeField] private Button story1Button;
    [SerializeField] private Button story2Button;
    [SerializeField] private TextMeshProUGUI story2Label;
    [SerializeField] private TextMeshProUGUI story2LockedHint;

    [Header("Szenen")]
    [SerializeField] private string story1Scene = "HouseScene";
    [SerializeField] private string story2Scene = "GasStationScene";

    [Header("Farben")]
    [SerializeField] private Color unlockedTextColor = new Color(0.577f, 0.169f, 0.169f, 1f);
    [SerializeField] private Color lockedTextColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CloseStorySelect();
    }

    // ==================================================
    // BUTTONS
    // ==================================================

    public void OpenStorySelect()
    {
        SetMainMenuVisible(false);

        if (storySelectPanel != null)
            storySelectPanel.SetActive(true);

        RefreshLockState();
    }

    public void CloseStorySelect()
    {
        if (storySelectPanel != null)
            storySelectPanel.SetActive(false);

        SetMainMenuVisible(true);
    }

    public void PlayStory1()
    {
        SceneLoader.Load(story1Scene);
    }

    public void PlayStory2()
    {
        if (!StoryProgress.IsStory1Completed())
            return;

        SceneLoader.Load(story2Scene);
    }

    // ==================================================
    // SPERRE
    // ==================================================

    private void RefreshLockState()
    {
        bool unlocked = StoryProgress.IsStory1Completed();

        if (story2Button != null)
            story2Button.interactable = unlocked;

        if (story2Label != null)
            story2Label.color = unlocked ? unlockedTextColor : lockedTextColor;

        if (story2LockedHint != null)
            story2LockedHint.gameObject.SetActive(!unlocked);
    }

    private void SetMainMenuVisible(bool visible)
    {
        if (mainMenuObjects == null)
            return;

        foreach (GameObject go in mainMenuObjects)
        {
            if (go != null)
                go.SetActive(visible);
        }
    }

    // ==================================================
    // TESTEN (Rechtsklick auf die Komponente im Inspector)
    // ==================================================

    [ContextMenu("Story 2 freischalten (Test)")]
    private void DebugUnlockStory2()
    {
        StoryProgress.CompleteStory1();
        RefreshLockState();
    }

    [ContextMenu("Fortschritt zuruecksetzen (Test)")]
    private void DebugResetProgress()
    {
        StoryProgress.ResetProgress();
        RefreshLockState();
    }
}
