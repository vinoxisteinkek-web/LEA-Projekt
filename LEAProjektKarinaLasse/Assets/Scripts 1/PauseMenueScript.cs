using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuScript : MonoBehaviour
{
    public GameObject pauseMenu;

    public Slider sensitivitySlider;
    public Slider volumeSlider;

    // Optional – Sensitivität läuft jetzt über GameSettings,
    // die Referenz wird nicht mehr zwingend gebraucht (z.B. im Hauptmenü).
    public CameraController cameraController;

    private bool isPaused = false;

    [SerializeField]
    private TextMeshProUGUI closeText;

    private void Start()
    {
        if (closeText != null) closeText.text = "Close";
        pauseMenu.SetActive(false);

        // Maus-Sensitivität
        sensitivitySlider.minValue = 50f;
        sensitivitySlider.maxValue = 400f;
        sensitivitySlider.SetValueWithoutNotify(GameSettings.Sensitivity);

        // Lautstärke
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.SetValueWithoutNotify(GameSettings.Volume);

        sensitivitySlider.onValueChanged.AddListener(ChangeSensitivity);
        volumeSlider.onValueChanged.AddListener(ChangeVolume);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(!isPaused)
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        // Slider immer mit den aktuellen globalen Werten synchronisieren
        sensitivitySlider.SetValueWithoutNotify(GameSettings.Sensitivity);
        volumeSlider.SetValueWithoutNotify(GameSettings.Volume);

        pauseMenu.SetActive(true);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;

        pauseMenu.SetActive(false);

        Time.timeScale = 1f;

        if(SceneManager.GetActiveScene().name != "MenueScene")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ChangeSensitivity(float value)
    {
        GameSettings.Sensitivity = value; // speichert + informiert alle CameraController
    }

    private void ChangeVolume(float value)
    {
        GameSettings.Volume = value; // speichert + setzt AudioListener.volume
    }
}
