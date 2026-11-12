using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    public Transform _camera;
    public Transform hand;

    // Wird aus GameSettings geladen (szenenübergreifend)
    public float cameraSensitivity = GameSettings.DefaultSensitivity;

    // Mausbewegung ist bereits "pro Frame" – daher KEIN Time.deltaTime mehr.
    // Dieser Faktor rechnet die alten Slider-Werte (50–400) so um,
    // dass sie sich wie vorher bei ca. 100 FPS anfühlen – nur jetzt bei jeder Framerate gleich.
    private const float SensitivityScale = 0.01f;

    private float rotation_x_axis;
    private float rotation_y_axis;

    void Awake()
    {
        cameraSensitivity = GameSettings.Sensitivity;
    }

    void OnEnable()
    {
        GameSettings.OnSettingsChanged += ApplySettings;
    }

    void OnDisable()
    {
        GameSettings.OnSettingsChanged -= ApplySettings;
    }

    private void ApplySettings()
    {
        cameraSensitivity = GameSettings.Sensitivity;
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name != "MenueScene")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        CreateCrosshair();
    }

    void Update()
    {
        // Im Pausemenü (timeScale = 0) Kamera nicht drehen
        if (Time.timeScale == 0f)
            return;

        // Mausbewegung (framerate-unabhängig, ohne deltaTime)
        float mouseX = Input.GetAxisRaw("Mouse X") * cameraSensitivity * SensitivityScale;
        float mouseY = Input.GetAxisRaw("Mouse Y") * cameraSensitivity * SensitivityScale;

        // Rotation speichern
        rotation_y_axis += mouseX;
        rotation_x_axis -= mouseY;

        // Nach oben/unten begrenzen
        rotation_x_axis = Mathf.Clamp(rotation_x_axis, -90f, 90f);

        // Kopf drehen (links/rechts)
        transform.localRotation = Quaternion.Euler(0f, rotation_y_axis, 0f);

        // Kamera drehen (hoch/runter)
        _camera.localRotation = Quaternion.Euler(rotation_x_axis, 0f, 0f);

        // Hand ebenfalls ausrichten
        hand.localRotation = Quaternion.Euler(rotation_x_axis, rotation_y_axis, 0f);
    }

    // =================================
    // FADENKREUZ
    // Ersetzt das alte OnGUI (lief mehrmals pro Frame und erzeugte Garbage).
    // Wird einmal beim Start als UI-Image erzeugt.
    // =================================
    private void CreateCrosshair()
    {
        const float size = 4f;

        GameObject canvasGO = new GameObject("CrosshairCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100; // unter anderen UIs (z.B. Pausemenü)

        GameObject dot = new GameObject("Crosshair");
        dot.transform.SetParent(canvasGO.transform, false);
        Image img = dot.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.6f);
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);
    }
}
