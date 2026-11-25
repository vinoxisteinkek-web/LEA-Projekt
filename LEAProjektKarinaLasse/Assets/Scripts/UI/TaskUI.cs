using UnityEngine;
using TMPro;

public class TaskUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerController player;

    [Header("Task Texts")]
    [SerializeField] private TextMeshProUGUI lightTaskText;
    [SerializeField] private TextMeshProUGUI taskListText;

    [SerializeField] private TextMeshProUGUI radioOnTaskText;

    [SerializeField] private TextMeshProUGUI dirtTaskText;
    [SerializeField] private TextMeshProUGUI puddleTaskText;
    [SerializeField] private TextMeshProUGUI trashTaskText;

    [SerializeField] private TextMeshProUGUI radioOffTaskText;
    [SerializeField] private TextMeshProUGUI lightOffTaskText;

    [SerializeField] private TextMeshProUGUI goHomeTaskText;


    [Header("Task Amounts")]
    [SerializeField] private int maxDirt = 3;
    [SerializeField] private int maxPuddles = 3;
    [SerializeField] private int maxTrash = 3;


    private bool lightTaskActive = true;
    private bool taskListActive = false;

    private bool radioOnTaskActive = false;
    private bool mainTasksActive = false;

    private bool radioOffTaskActive = false;
    private bool lightOffTaskActive = false;

    private bool goHomeTaskActive = false;


    private void Start()
    {
        // ---------------------------------
        // ALLES ZUERST AUSBLENDEN
        // ---------------------------------

        if (lightTaskText != null)
            lightTaskText.gameObject.SetActive(false);

        if (taskListText != null)
            taskListText.gameObject.SetActive(false);

        if (radioOnTaskText != null)
            radioOnTaskText.gameObject.SetActive(false);

        if (dirtTaskText != null)
            dirtTaskText.gameObject.SetActive(false);

        if (puddleTaskText != null)
            puddleTaskText.gameObject.SetActive(false);

        if (trashTaskText != null)
            trashTaskText.gameObject.SetActive(false);

        if (radioOffTaskText != null)
            radioOffTaskText.gameObject.SetActive(false);

        if (lightOffTaskText != null)
            lightOffTaskText.gameObject.SetActive(false);

        if (goHomeTaskText != null)
            goHomeTaskText.gameObject.SetActive(false);


        // ---------------------------------
        // ERSTE AUFGABE
        // ---------------------------------

        ShowLightTask();
    }


    // =====================================
    // LICHT AN
    // =====================================

    public void ShowLightTask()
    {
        lightTaskActive = true;

        if (lightTaskText != null)
        {
            lightTaskText.gameObject.SetActive(true);
            lightTaskText.text = "Turn on the Lights";
        }
    }


    public void CompleteLightTask()
    {
        lightTaskActive = false;

        if (lightTaskText != null)
        {
            lightTaskText.text = "Turn on the Lights";
            lightTaskText.gameObject.SetActive(false);
        }

        ShowTaskListTask();
    }


    // =====================================
    // BOSS-NOTIZ
    // =====================================

    private void ShowTaskListTask()
    {
        taskListActive = true;

        if (taskListText != null)
        {
            taskListText.gameObject.SetActive(true);
            taskListText.text = "Read the note from your boss";
        }
    }


    public void CompleteTaskListTask()
    {
        taskListActive = false;

        if (taskListText != null)
        {
            taskListText.text = "Read the note from your boss";
            taskListText.gameObject.SetActive(false);
        }
    }


    // =====================================
    // RADIO AN
    // =====================================

    public void ShowRadioOnTask()
    {
        radioOnTaskActive = true;

        if (radioOnTaskText != null)
        {
            radioOnTaskText.gameObject.SetActive(true);
            radioOnTaskText.text = "Turn on the Radio";
        }
    }


    public void CompleteRadioOnTask()
    {
        radioOnTaskActive = false;

        if (radioOnTaskText != null)
        {
            radioOnTaskText.text = "Turn on the Radio";
            radioOnTaskText.gameObject.SetActive(false);
        }
    }


    // =====================================
    // HAUPTAUFGABEN
    // =====================================

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

        if (AreAllTasksComplete())
        {
            dirtTaskText.gameObject.SetActive(false);
            puddleTaskText.gameObject.SetActive(false);
            trashTaskText.gameObject.SetActive(false);
            ShowRadioOffTask();
        }
    }


    private void UpdateDirtTask()
    {
        int current = player.dirtCount;

        if (dirtTaskText == null)
            return;

        dirtTaskText.text =
            "Remove the Dust       " +
            Mathf.Min(current, maxDirt) + "/" + maxDirt;
    }


    private void UpdatePuddleTask()
    {
        int current = player.puddleCount;

        if (puddleTaskText == null)
            return;

        puddleTaskText.text =
            "Clean the Floor       " +
            Mathf.Min(current, maxPuddles) + "/" + maxPuddles;
    }


    private void UpdateTrashTask()
    {
        int current = player.trashCount;

        if (trashTaskText == null)
            return;

        trashTaskText.text =
            "Take out the Trash    " +
            Mathf.Min(current, maxTrash) + "/" + maxTrash;
    }


    // =====================================
    // SIND HAUPTAUFGABEN FERTIG?
    // =====================================

    public bool AreAllTasksComplete()
    {
        if (player == null)
            return false;

        return player.dirtCount >= maxDirt &&
               player.puddleCount >= maxPuddles &&
               player.trashCount >= maxTrash;
    }


    // =====================================
    // RADIO AUS
    // =====================================

    public void ShowRadioOffTask()
    {
        if (!AreAllTasksComplete())
            return;

        if (radioOffTaskActive)
            return;

        radioOffTaskActive = true;

        if (radioOffTaskText != null)
        {
            radioOffTaskText.gameObject.SetActive(true);
            radioOffTaskText.text = "Turn off the Radio";
        }
    }


    public void CompleteRadioOffTask()
    {
        radioOffTaskActive = false;

        if (radioOffTaskText != null)
        {
            radioOffTaskText.text = "Turn off the Radio";
            radioOffTaskText.gameObject.SetActive(false);
        }

        ShowLightOffTask();
    }


    // =====================================
    // LICHT AUS
    // =====================================

    public void ShowLightOffTask()
    {
        if (lightOffTaskActive)
            return;

        lightOffTaskActive = true;

        if (lightOffTaskText != null)
        {
            lightOffTaskText.gameObject.SetActive(true);
            lightOffTaskText.text = "Turn off the Light";
        }
    }


    public void CompleteLightOffTask()
    {
        lightOffTaskActive = false;

        if (lightOffTaskText != null)
        {
            lightOffTaskText.text = "Turn off the Light";
            lightOffTaskText.gameObject.SetActive(false);
        }

        ShowGoHomeTask();
    }


    // =====================================
    // NACH HAUSE
    // =====================================

    public void ShowGoHomeTask()
    {
        goHomeTaskActive = true;

        if (goHomeTaskText != null)
        {
            goHomeTaskText.gameObject.SetActive(true);
            goHomeTaskText.text = "Go back home";
        }
    }


    public void CompleteGoHomeTask()
    {
        if (!goHomeTaskActive)
            return;

        goHomeTaskActive = false;

        if (goHomeTaskText != null)
        {
            goHomeTaskText.text = "Go back home";
        }
    }
}