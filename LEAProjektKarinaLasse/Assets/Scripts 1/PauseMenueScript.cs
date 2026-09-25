using UnityEngine;
using UnityEngine.UI;

public class PauseMenuScript : MonoBehaviour
{
    public GameObject pauseMenu;

    public Slider sensitivitySlider;
    public Slider volumeSlider;

    public CameraController cameraController;

    private bool isPaused = false;

    private void Start()
    {
        pauseMenu.SetActive(false);

        // Maus-Sensitivität
        sensitivitySlider.minValue = 50f;
        sensitivitySlider.maxValue = 400f;
        sensitivitySlider.value = cameraController.cameraSensitivity;

        // Lautstärke
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = AudioListener.volume;

        sensitivitySlider.onValueChanged.AddListener(ChangeSensitivity);
        volumeSlider.onValueChanged.AddListener(ChangeVolume);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    private void PauseGame()
    {
        isPaused = true;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ChangeSensitivity(float value)
    {
        cameraController.cameraSensitivity = value;
    }

    private void ChangeVolume(float value)
    {
        AudioListener.volume = value;
    }
}