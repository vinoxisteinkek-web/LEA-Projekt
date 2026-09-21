using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform _camera;
    public Transform hand;
    public float cameraSensitivity = 200f;
    public float cameraAcceleration = 5f;

    private float rotation_x_axis;
    private float rotation_y_axis;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        rotation_x_axis += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
        rotation_y_axis += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;

        rotation_x_axis = Mathf.Clamp(rotation_x_axis, -90f, 90f);

        hand.localRotation = Quaternion.Euler(-rotation_x_axis, rotation_y_axis, 0);

        transform.localRotation = Quaternion.Lerp(transform.localRotation,
            Quaternion.Euler(0, rotation_y_axis, 0), cameraAcceleration * Time.deltaTime);

        _camera.localRotation = Quaternion.Lerp(_camera.localRotation,
            Quaternion.Euler(-rotation_x_axis, 0, 0), cameraAcceleration * Time.deltaTime);
    }
}
