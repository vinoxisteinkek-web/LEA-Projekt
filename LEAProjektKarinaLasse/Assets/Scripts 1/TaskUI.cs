using UnityEngine;
using TMPro;

public class TaskUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerController player;

    [Header("Task Texts")]
    [SerializeField] private TextMeshProUGUI lightTaskText;
    [SerializeField] private TextMeshProUGUI taskListText;

    [SerializeField] private TextMeshProUGUI dirtTaskText;
    [SerializeField] private TextMeshProUGUI puddleTaskText;
    [SerializeField] private TextMeshProUGUI trashTaskText;

    [SerializeField] private TextMeshProUGUI goHomeTaskText;

    [Header("Task Amounts")]
    [SerializeField] private int maxDirt = 3;
    [SerializeField] private int maxPuddles = 3;
    [SerializeField] private int maxTrash = 3;

    private bool lightTaskActive = true;
    private bool taskListActive = false;
    private bool mainTasksActive = false;
    private bool goHomeTaskActive = false;


    private void Start()
    {
        // Am Anfang nur Licht-Aufgabe anzeigen
        ShowLightTask();

        if (taskListText != null)
            taskListText.gameObject.SetActive(false);

        if (dirtTaskText != null)
            dirtTaskText.gameObject.SetActive(false);

        if (puddleTaskText != null)
            puddleTaskText.gameObject.SetActive(false);

        if (trashTaskText != null)
            trashTaskText.gameObject.SetActive(false);

        if (goHomeTaskText != null)
            goHomeTaskText.gameObject.SetActive(false);
    }


    // ==================================================
    // LICHT-AUFGABE
    // ==================================================

    public void ShowLightTask()
    {
        lightTaskActive = true;

        if (lightTaskText != null)
        {
            lightTaskText.gameObject.SetActive(true);
            lightTaskText.text = "☐ Mache das Licht an";
        }
    }


    public void CompleteLightTask()
    {
        lightTaskActive = false;

        if (lightTaskText != null)
        {
            lightTaskText.text = "✓ Mache das Licht an";
        }

        ShowTaskListTask();
    }


    // ==================================================
    // NOTIZ-AUFGABE
    // ==================================================

    private void ShowTaskListTask()
    {
        taskListActive = true;

        if (taskListText != null)
        {
            taskListText.gameObject.SetActive(true);
            taskListText.text = "☐ Lies die Notiz vom Boss";
        }
    }


    public void CompleteTaskListTask()
    {
        taskListActive = false;

        if (taskListText != null)
        {
            taskListText.text = "✓ Lies die Notiz vom Boss";
        }
    }


    // ==================================================
    // HAUPTAUFGABEN
    // ==================================================

    public void ShowMainTasks()
    {
        mainTasksActive = true;

        if (dirtTaskText != null)
            dirtTaskText.gameObject.SetActive(true);

        if (puddleTaskText != null)
            puddleTaskText.gameObject.SetActive(true);

        if (trashTaskText != null)
            trashTaskText.gameObject.SetActive(true);

        UpdateTasks();
    }


    public void UpdateTasks()
    {
        if (player == null)
            return;

        if (!mainTasksActive)
            return;

        UpdateDirtTask();
        UpdatePuddleTask();
        UpdateTrashTask();
    }


    // ==================================================
    // DIRT
    // ==================================================

    private void UpdateDirtTask()
    {
        int current = player.dirtCount;

        if (current >= maxDirt)
        {
            dirtTaskText.text =
                "✓ Remove the Dust       " +
                maxDirt + "/" + maxDirt;
        }
        else
        {
            dirtTaskText.text =
                "☐ Remove the Dust       " +
                current + "/" + maxDirt;
        }
    }


    // ==================================================
    // PUDDLE
    // ==================================================

    private void UpdatePuddleTask()
    {
        int current = player.puddleCount;

        if (current >= maxPuddles)
        {
            puddleTaskText.text =
                "✓ Clean the Floor       " +
                maxPuddles + "/" + maxPuddles;
        }
        else
        {
            puddleTaskText.text =
                "☐ Clean the Floor       " +
                current + "/" + maxPuddles;
        }
    }


    // ==================================================
    // TRASH
    // ==================================================

    private void UpdateTrashTask()
    {
        int current = player.trashCount;

        if (current >= maxTrash)
        {
            trashTaskText.text =
                "✓ Take out the Trash    " +
                maxTrash + "/" + maxTrash;
        }
        else
        {
            trashTaskText.text =
                "☐ Take out the Trash    " +
                current + "/" + maxTrash;
        }
    }


    // ==================================================
    // ALLE 3 HAUPTTASKS FERTIG?
    // ==================================================

    public bool AreAllTasksComplete()
    {
        if (player == null)
            return false;

        return player.dirtCount >= maxDirt &&
               player.puddleCount >= maxPuddles &&
               player.trashCount >= maxTrash;
    }


    // ==================================================
    // GO HOME
    // ==================================================

    public void ShowGoHomeTask()
    {
        goHomeTaskActive = true;

        if (goHomeTaskText != null)
        {
            goHomeTaskText.gameObject.SetActive(true);
            goHomeTaskText.text = "☐ Go back home";
        }
    }


    public void CompleteGoHomeTask()
    {
        if (!goHomeTaskActive)
            return;

        goHomeTaskActive = false;

        if (goHomeTaskText != null)
        {
            goHomeTaskText.text = "✓ Go back home";
        }
    }
}