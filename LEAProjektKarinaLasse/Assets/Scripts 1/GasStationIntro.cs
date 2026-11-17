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

    [Header("Einfuehrung (wie bei Story 1)")]
    public bool playIntroStory = true;
    public TextMeshProUGUI introText;                // weisser Text mittig auf Schwarz
    [TextArea(3, 8)]
    public string introStory =
        "Nobody believed me when I told them what happened that night. " +
        "It was November 1998, just a few weeks after that girl went missing in Emschurches. " +
        "I was driving home late on the old country road... and I never should have stopped.";
    public float introTypeSpeed = 0.035f;
    public float introHoldAfterTyping = 3.5f;

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
        HideText(introText);

        // Von Anfang an schwarz, damit man vor der Einfuehrung nichts sieht
        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            blackScreen.alpha = 1f;
        }

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
        // Einfuehrung auf schwarzem Bildschirm (wie bei Story 1)
        if (playIntroStory && introText != null && !string.IsNullOrEmpty(introStory))
        {
            yield return new WaitForSeconds(1f);
            yield return TypeIntro(introStory);
            yield return new WaitForSeconds(introHoldAfterTyping);
            HideText(introText);
            yield return new WaitForSeconds(0.5f);
        }

        // Aus dem Schwarz einblenden, das Auto rollt schon
        car.SetSpeed(startSpeed);
        yield return Fade(1f, 0f, startFadeDuration);

        car.controlsEnabled = true;
        StartCoroutine(ShowHint(
            InputBindings.GetKeyName(GameAction.MoveForward) + " / " + InputBindings.GetKeyName(GameAction.MoveBackward) + " - Gas / Bremse      " +
            InputBindings.GetKeyName(GameAction.MoveLeft) + " / " + InputBindings.GetKeyName(GameAction.MoveRight) + " - Lenken      " +
            InputBindings.GetKeyName(GameAction.Handbrake) + " - Handbremse      Maus - Umschauen",
            controlsHintDuration));

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
            // Aussteigen geht, sobald das Auto steht und eine Zapfsaeule
            // in Schlauch-Reichweite ist. Die Zone dient nur noch dazu,
            // einen Hinweis zu zeigen, wenn man an der Tankstelle zu weit weg haelt.
            bool stopped = Mathf.Abs(car.CurrentSpeed) < 0.5f;
            float pumpDist = NearestPumpDistance(out float pumpReach);
            bool nearPump = pumpDist <= pumpReach;
            bool atStation = (stopZone != null && stopZone.bounds.Contains(car.transform.position))
                             || pumpDist <= pumpReach + 10f;
            bool canExit = stopped && nearPump;

            if (stopped && atStation && !nearPump)
            {
                ShowPrompt("Näher an die Zapfsäule fahren");
            }
            else if (canExit)
            {
                ShowPrompt(InputBindings.FormatPrompt("[E] Aussteigen"));
                if (InputBindings.GetDown(GameAction.Interact))
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
    // ZAPFSAEULEN-NAEHE
    // =================================
    private FuelPumpInteractable[] pumps;

    // Abstand zur naechsten Zapfsaeule und deren Schlauch-Reichweite
    private float NearestPumpDistance(out float reach)
    {
        if (pumps == null || pumps.Length == 0)
            pumps = FindObjectsByType<FuelPumpInteractable>(FindObjectsSortMode.None);
        reach = 5f;
        if (pumps.Length == 0) return 0f; // keine Saeulen gefunden -> nicht blockieren
        float best = float.MaxValue;
        foreach (var p in pumps)
        {
            if (p == null) continue;
            float d = p.CarDistance(car);
            if (d < best) { best = d; reach = p.maxCarDistance; }
        }
        return best;
    }

    // Mittelpunkt und halbe Breite/Laenge des Autos (Welt-Meter) aus dem groessten BoxCollider
    private void GetCarFootprint(out Vector3 center, out float halfWidth, out float halfLength)
    {
        center = car.transform.position;
        halfWidth = 1.0f;
        halfLength = 2.6f;
        BoxCollider best = null;
        foreach (var b in car.GetComponents<BoxCollider>())
            if (!b.isTrigger && (best == null || b.size.x * b.size.z > best.size.x * best.size.z)) best = b;
        if (best == null) return;
        Vector3 s = car.transform.lossyScale;
        Vector3 wc = car.transform.TransformPoint(best.center);
        center = new Vector3(wc.x, car.transform.position.y, wc.z);
        halfWidth = best.size.x * s.x * 0.5f;
        halfLength = best.size.z * s.z * 0.5f;
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

        // Freien Platz neben dem Auto suchen (Fahrerseite zuerst).
        // Abstaende kommen aus dem Auto-Collider, damit sie auch bei
        // skaliertem Auto stimmen.
        GetCarFootprint(out Vector3 c, out float halfWidth, out float halfLength);
        const float margin = 0.6f; // Spieler-Radius + etwas Luft
        Vector3[] candidates =
        {
            c - car.transform.right * (halfWidth + margin),
            c + car.transform.right * (halfWidth + margin),
            c - car.transform.forward * (halfLength + margin),
            c + car.transform.forward * (halfLength + margin)
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

    public void HideTask()
    {
        HideText(taskText);
    }

    public void ShowTask(string text)
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

    // Einfuehrungstext: Buchstabe fuer Buchstabe mit Stimme (wie StoryMessageScript in Story 1)
    private IEnumerator TypeIntro(string message)
    {
        introText.text = message;
        introText.maxVisibleCharacters = 0;
        introText.gameObject.SetActive(true);

        if (playerVoiceAudioSource != null && playerTalking != null)
        {
            playerVoiceAudioSource.clip = playerTalking;
            playerVoiceAudioSource.loop = true;
            playerVoiceAudioSource.Play();
        }

        var wait = new WaitForSeconds(introTypeSpeed);
        for (int i = 1; i <= message.Length; i++)
        {
            introText.maxVisibleCharacters = i;
            yield return wait;
        }
        introText.maxVisibleCharacters = int.MaxValue;

        if (playerVoiceAudioSource != null) playerVoiceAudioSource.Stop();
    }

    // Gleiche Darstellung wie im Supermarkt: orange, Buchstabe für Buchstabe
    public IEnumerator PlayerSay(string message)
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
