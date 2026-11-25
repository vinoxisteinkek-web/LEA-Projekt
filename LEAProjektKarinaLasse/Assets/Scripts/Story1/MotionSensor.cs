using UnityEngine;

public class MotionSensor : MonoBehaviour
{
    private DoorController doorController;

    void Start()
    {
        doorController = GetComponentInParent<DoorController>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            doorController.OnMotionDetected();
        }
    }
}