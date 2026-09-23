using UnityEngine;
using TMPro;

public class TaskUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerController player;

    [Header("Task Texts")]
    [SerializeField] private TextMeshProUGUI dirtTaskText;
    [SerializeField] private TextMeshProUGUI puddleTaskText;
    [SerializeField] private TextMeshProUGUI trashTaskText;

    [Header("Task Amounts")]
    [SerializeField] private int maxDirt = 3;
    [SerializeField] private int maxPuddles = 3;
    [SerializeField] private int maxTrash = 3;

    private void Start()
    {
        UpdateTasks();
    }

    public void UpdateTasks()
    {
        UpdateDirtTask();
        UpdatePuddleTask();
        UpdateTrashTask();
    }

    private void UpdateDirtTask()
    {
        int current = player.dirtCount;

        if (current >= maxDirt)
        {
            dirtTaskText.text =
                "Schmutz entfernen    " +
                maxDirt + "/" + maxDirt;
        }
        else
        {
            dirtTaskText.text =
                "Schmutz entfernen    " +
                current + "/" + maxDirt;
        }
    }

    private void UpdatePuddleTask()
    {
        int current = player.puddleCount;

        if (current >= maxPuddles)
        {
            puddleTaskText.text =
                "Boden wischen        " +
                maxPuddles + "/" + maxPuddles;
        }
        else
        {
            puddleTaskText.text =
                "Boden wischen        " +
                current + "/" + maxPuddles;
        }
    }

    private void UpdateTrashTask()
    {
        int current = player.trashCount;

        if (current >= maxTrash)
        {
            trashTaskText.text =
                "Müll rausbringen     " +
                maxTrash + "/" + maxTrash;
        }
        else
        {
            trashTaskText.text =
                "Müll rausbringen     " +
                current + "/" + maxTrash;
        }
    }
}