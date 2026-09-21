using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTEstScript : MonoBehaviour
{
   
    public float speed = 5f;
    public float sprintSpeed = 9f;
    public float mouseSensitivity = 0.1f;

    public Transform cameraTransform;

    private Rigidbody rb;
    private float xRotation = 0f;

    private Vector2 moveInput;
    private Vector2 mouseInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // WASD
        moveInput = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            moveInput.y += 1;

        if (Keyboard.current.sKey.isPressed)
            moveInput.y -= 1;

        if (Keyboard.current.dKey.isPressed)
            moveInput.x += 1;

        if (Keyboard.current.aKey.isPressed)
            moveInput.x -= 1;


        // Maus
        mouseInput = Mouse.current.delta.ReadValue();

        float mouseX = mouseInput.x * mouseSensitivity;
        float mouseY = mouseInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation =
            Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    void FixedUpdate()
    {
        Vector3 movement =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        movement.Normalize();

        float currentSpeed = speed;

        // Shift = Sprint
        if (Keyboard.current.leftShiftKey.isPressed)
        {
            currentSpeed = sprintSpeed;
        }

        rb.MovePosition(
            rb.position +
            movement * currentSpeed * Time.fixedDeltaTime
        );
    }
}
