using UnityEngine;
using TMPro;

/// <summary>
/// Kleines Aufgabenfenster (oben links) für die HouseScene –
/// gleicher Stil wie das TaskPanel in der SupermarketScene.
/// </summary>
public class HouseTaskUI : MonoBehaviour
{
    [SerializeField] private GameObject taskPanel;
    [SerializeField] private TextMeshProUGUI currentTaskText;

    private void Awake()
    {
        // Während des Intros (schwarzer Bildschirm) noch nichts anzeigen
        HideTask();
    }

    /// <summary>Zeigt die aktuelle Aufgabe an.</summary>
    public void ShowTask(string task)
    {
        if (currentTaskText != null)
            currentTaskText.text = task;

        if (taskPanel != null)
            taskPanel.SetActive(true);
    }

    /// <summary>Blendet das Fenster aus.</summary>
    public void HideTask()
    {
        if (taskPanel != null)
            taskPanel.SetActive(false);
    }
}
