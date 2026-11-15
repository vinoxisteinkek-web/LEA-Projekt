using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// =====================================================
// TANKSTELLE - ANFANG
// 1. Spieler fährt nachts über die Landstraße
// 2. Tankwarnleuchte geht an -> "Tank ist fast leer"
// 3. Tankstelle taucht auf -> dort an der Zapfsäule halten
// 4. [E] Aussteigen -> Spieler läuft zu Fuß weiter
// Danach wird onPlayerExitedCar ausgelöst (Anschluss für den nächsten Teil).
// =====================================================
public class GasStationIntro : MonoBehaviour
{
    [Header("Objekte")]
    public CarController car;
    public GameObject player;
    public Transform gasStation;
    public Collider stopZone;               // Bereich an den Zapfsäulen

    [Header("UI")]
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI taskText;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI hintText;
    public CanvasGroup blackScreen;

    [Header("Stimme")]
    public AudioSource playerVoiceAudioSource;
    public AudioClip playerTalking;

    [Header("Timing")]
    public float startFadeDuration = 2.5f;
    public float startSpeed = 13f;                  // Auto rollt beim Einblenden schon (m/s)
    public float controlsHintDuration = 6f;
    public float lowFuelAfterSeconds = 7f;          // Fahrzeit bis die Lampe angeht
    public float spotStationDistance = 120f;        // ab hier "Da vorne ist eine Tankstelle!"
    public float textSpeed = 0.045f;
    public float dialogueEndDelay = 1.5f;

    [Header("Danach")]
    public UnityEvent onPlayerExitedCar;

    private bool playerOutOfCar;
    private float playerPivotHeight = 1f;           // Abstand Pivot <-> Boden beim Spieler

    void Start()
    {
        if (player != null)
        {
            // Höhe des Spieler-Pivots über dem Boden merken (zum Aussteigen)
            if (GroundY(player.transform.position, out float gy))
                playerPivotHeight = player.transform.position.y - gy;

            player.SetActive(false);
        }

        HideText(dialogueText);
        HideText(promptText);
        HideText(hintText);
        HideText(taskText);

        if (car != null)
        {
            car.controlsEnabled = false;
            car.engineRunning = true;
            car.LowFuelWarning = false;
            if (car.driverCamera != null) car.driverCamera.gameObject.SetActive(true);
        }

        StartCoroutine(Sequence());
    }

    // =================================
    // ABLAUF
    // =================================
    private IEnumerator Sequence()
    {
        // Aus dem Schwarz einblenden, das Auto rollt schon
        car.SetSpeed(startSpeed);
        yield return Fade(1f, 0f, startFadeDuration);

        car.controlsEnabled = true;
        StartCoroutine(ShowHint("W / S - Gas / Bremse      A / D - Lenken      Maus - Umschauen", controlsHintDuration));

        // Eine Weile fahren lassen
        float driven = 0f;
        while (driven < lowFuelAfterSeconds)
        {
            if (Mathf.Abs(car.CurrentSpeed) > 2f) driven += Time.deltaTime;
            yield return null;
        }

        // Tankwarnleuchte
        car.LowFuelWarning = true;
        yield return new WaitForSeconds(1.5f);
        yield return PlayerSay("Huh? Is that the fuel light?");
        yield return new WaitForSeconds(0.5f);
        yield return PlayerSay("Great... the tank is almost empty. There has to be a gas station somewhere around here.");
        ShowTask("Eine Tankstelle finden");

        // Tankstelle in Sicht
        while (gasStation != null && Vector3.Distance(car.transform.position, gasStation.position) > spotStationDistance)
            yield return null;

        yield return PlayerSay("There! A gas station. Lucky me.");
        ShowTask("An der Zapfsäule halten");

        // An der Zapfsäule anhalten und aussteigen
        while (!playerOutOfCar)
        {
            bool canExit = stopZone != null
                && stopZone.bounds.Contains(car.transform.position)
                && Mathf.Abs(car.CurrentSpeed) < 0.5f;

            if (canExit)
            {
                ShowPrompt("[E] Aussteigen");
                if (Input.GetKeyDown(KeyCode.E))
                {
                    HideText(promptText);
                    yield return ExitCar();
                }
            }
            else
            {
                HideText(promptText);
            }
            yield return null;
        }

        ShowTask("Das Auto volltanken");
        yield return new WaitForSeconds(0.8f);
        yield return PlayerSay("Okay. Fill it up and get home. This place gives me the creeps.");

        onPlayerExitedCar?.Invoke();
    }

    // =================================
    // AUSSTEIGEN
    // =================================
    private IEnumerator ExitCar()
    {
        car.controlsEnabled = false;
        car.StopImmediately();

        yield return Fade(0f, 1f, 0.6f);

        car.engineRunning = false;
        car.LowFuelWarning = false;

        // Freien Platz neben dem Auto suchen (Fahrerseite zuerst)
        Vector3 c = car.transform.position;
        Vector3[] candidates =
        {
            c - car.transform.right * 1.6f,
            c + car.transform.right * 1.6f,
            c - car.transform.forward * 3.2f,
            c + car.transform.forward * 3.2f
        };

        Vector3 exitPos = candidates[0];
        foreach (var p in candidates)
        {
            Vector3 bottom = p + Vector3.up * 0.4f;
            Vector3 top = p + Vector3.up * 1.7f;
            if (!Physics.CheckCapsule(bottom, top, 0.35f, ~0, QueryTriggerInteraction.Ignore))
            {
                exitPos = p;
                break;
            }
        }

        if (GroundY(exitPos, out float ey))
            exitPos.y = ey;
        exitPos.y += playerPivotHeight;

        if (car.driverCamera != null) car.driverCamera.gameObject.SetActive(false);

        player.transform.SetPositionAndRotation(exitPos, Quaternion.Euler(0f, car.transform.eulerAngles.y, 0f));
        player.SetActive(true);

        playerOutOfCar = true;

        yield return new WaitForSeconds(0.3f);
        yield return Fade(1f, 0f, 1f);
    }

    // =================================
    // UI
    // =================================
    private IEnumerator Fade(float from, float to, float duration)
    {
        if (blackScreen == null)
            yield break;

        blackScreen.gameObject.SetActive(true);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            blackScreen.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        blackScreen.alpha = to;
        if (to <= 0f) blackScreen.gameObject.SetActive(false);
    }

    private IEnumerator ShowHint(string text, float duration)
    {
        if (hintText == null) yield break;
        hintText.text = text;
        hintText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        hintText.gameObject.SetActive(false);
    }

    private void ShowTask(string text)
    {
        if (taskText == null) return;
        taskText.text = "Aufgabe:\n<size=80%>" + text + "</size>";
        taskText.gameObject.SetActive(true);
    }

    private void ShowPrompt(string text)
    {
        if (promptText == null) return;
        if (!promptText.gameObject.activeSelf || promptText.text != text)
        {
            promptText.text = text;
            promptText.gameObject.SetActive(true);
        }
    }

    private void HideText(TextMeshProUGUI t)
    {
        if (t != null) t.gameObject.SetActive(false);
    }

    // Bodenhöhe unter einer Position (ignoriert Spieler und Auto)
    private bool GroundY(Vector3 pos, out float y)
    {
        y = 0f;
        bool found = false;
        float best = float.MaxValue;
        foreach (var h in Physics.RaycastAll(pos + Vector3.up * 2.5f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (player != null && h.collider.transform.IsChildOf(player.transform)) continue;
            if (car != null && h.collider.transform.IsChildOf(car.transform)) continue;
            if (h.distance < best)
            {
                best = h.distance;
                y = h.point.y;
                found = true;
            }
        }
        return found;
    }

    // Gleiche Darstellung wie im Supermarkt: orange, Buchstabe für Buchstabe
    private IEnumerator PlayerSay(string message)
    {
        if (dialogueText == null) yield break;

        dialogueText.text = message;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.gameObject.SetActive(true);

        if (playerVoiceAudioSource != null && playerTalking != null)
        {
            playerVoiceAudioSource.clip = playerTalking;
            playerVoiceAudioSource.loop = true;
            playerVoiceAudioSource.Play();
        }

        var wait = new WaitForSeconds(textSpeed);
        for (int i = 1; i <= message.Length; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return wait;
        }
        dialogueText.maxVisibleCharacters = int.MaxValue;

        if (playerVoiceAudioSource != null) playerVoiceAudioSource.Stop();

        yield return new WaitForSeconds(dialogueEndDelay + message.Length * 0.025f);
        dialogueText.gameObject.SetActive(false);
    }
}
