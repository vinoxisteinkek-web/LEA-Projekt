using UnityEngine;

public class DirtPile : MonoBehaviour
{
    private int sweepCount = 0;

    public PlayerController player;
    public TaskUI taskUI;

    private void Start()
    {
        Vector3 pos = transform.position;
        pos.y = 4.3f;
        transform.position = pos;
    }

    public void Sweep()
    {
        sweepCount++;

        if (sweepCount == 1)
        {
            Vector3 pos = transform.position;
            pos.y = 4.25f;
            transform.position = pos;
        }
        else if (sweepCount == 2)
        {
            Vector3 pos = transform.position;
            pos.y = 4.24f;
            transform.position = pos;
        }
        else if (sweepCount >= 3)
        {
            player.dirtCount++;

            gameObject.SetActive(false);

            TaskUI taskUI = FindFirstObjectByType<TaskUI>();

            if (taskUI != null)
            {
                taskUI.UpdateTasks();
            }
        }
    }
}