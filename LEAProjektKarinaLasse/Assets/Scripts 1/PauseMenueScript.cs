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

    // Cursor-Zustand vor dem Pausieren (z.B. frei waehrend eines Dialogs)
    private CursorLockMode previousLockState = CursorLockMode.Locked;
    private bool previousCursorVisible = false;

    [SerializeField]
    private TextMeshProUGUI closeText;

    // Button "Main Menu" im Pause-Menue-Prefab. Wird per Name gesucht,
    // damit er in jeder Szene ohne Zuweisung funktioniert.
    private const string MainMenuSceneName = "MenueScene";
    private const string MainMenuButtonName = "MainMenuButton";
    private Button mainMenuButton;

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

        SetupMainMenuButton();
    }

    private void SetupMainMenuButton()
    {
        foreach (Button b in pauseMenu.GetComponentsInChildren<Button>(true))
        {
            if (b.name == MainMenuButtonName)
            {
                mainMenuButton = b;
                break;
            }
        }

        if (mainMenuButton == null)
            return;

        // Im Hauptmenue selbst ergibt der Button keinen Sinn
        bool inMainMenu = SceneManager.GetActiveScene().name == MainMenuSceneName;
        mainMenuButton.gameObject.SetActive(!inMainMenu);

        mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    public void ReturnToMainMenu()
    {
        isPaused = false;
        pauseMenu.SetActive(false);

        // Zeit wieder normal laufen lassen, sonst bleibt das Hauptmenue eingefroren
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneLoader.Load(MainMenuSceneName);
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

        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

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
            // Cursor so wiederherstellen, wie er vor der Pause war
            Cursor.lockState = previousLockState;
            Cursor.visible = previousCursorVisible;
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
