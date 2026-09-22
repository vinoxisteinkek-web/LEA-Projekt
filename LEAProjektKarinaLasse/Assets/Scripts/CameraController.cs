using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform _camera;
    public Transform hand;

    public float cameraSensitivity = 200f;

    private float rotation_x_axis;
    private float rotation_y_axis;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Mausbewegung
        float mouseX = Input.GetAxisRaw("Mouse X") * cameraSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * cameraSensitivity * Time.deltaTime;

        // Rotation speichern
        rotation_y_axis += mouseX;
        rotation_x_axis -= mouseY;

        // Nach oben/unten begrenzen
        rotation_x_axis = Mathf.Clamp(rotation_x_axis, -90f, 90f);

        // Spieler drehen (links/rechts)
        transform.localRotation = Quaternion.Euler(0f, rotation_y_axis, 0f);

        // Kamera drehen (hoch/runter)
        _camera.localRotation = Quaternion.Euler(rotation_x_axis, 0f, 0f);

        // Hand/Kamera-Objekt ebenfalls ausrichten
        hand.localRotation = Quaternion.Euler(rotation_x_axis, rotation_y_axis, 0f);
    }
}