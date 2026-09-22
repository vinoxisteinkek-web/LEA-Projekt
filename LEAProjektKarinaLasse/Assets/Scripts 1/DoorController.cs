using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Transform doorLeft;
    [SerializeField] private Transform doorRight;
    [SerializeField] private float openDistance = 1.5f;
    [SerializeField] private float doorSpeed = 2f;
    [SerializeField] private float sensorCloseDelay = 2f;

    private bool isOpen = false;
    private float closeTimer = 0f;

    private Vector3 doorLeftClosedPos;
    private Vector3 doorRightClosedPos;

    void Start()
    {
        doorLeftClosedPos = doorLeft.localPosition;
        doorRightClosedPos = doorRight.localPosition;
    }

    void Update()
    {
        Vector3 leftTarget;
        Vector3 rightTarget;

        if (isOpen)
        {
            // Linke Tür nach links
            leftTarget = doorLeftClosedPos + Vector3.left * openDistance;

            // Rechte Tür nach rechts
            rightTarget = doorRightClosedPos + Vector3.right * openDistance;

            closeTimer -= Time.deltaTime;

            if (closeTimer <= 0f)
            {
                isOpen = false;
            }
        }
        else
        {
            leftTarget = doorLeftClosedPos;
            rightTarget = doorRightClosedPos;
        }

        doorLeft.localPosition = Vector3.MoveTowards(
            doorLeft.localPosition,
            leftTarget,
            doorSpeed * Time.deltaTime
        );

        doorRight.localPosition = Vector3.MoveTowards(
            doorRight.localPosition,
            rightTarget,
            doorSpeed * Time.deltaTime
        );
    }

    public void OnMotionDetected()
    {
        isOpen = true;
        closeTimer = sensorCloseDelay;
    }
}