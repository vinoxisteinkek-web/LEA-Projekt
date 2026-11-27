using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// =====================================================
// STORY 2 - TEIL 2: DER WATCHTOWER
//  1. Spieler fährt nachts weiter (Auto rollt beim Einblenden)
//  2. Er sieht das rote Auto von der Tankstelle am Straßenrand
//  3. Anhalten -> [E] Aussteigen -> nachsehen, wo der Fahrer ist
//  4. Zaun (Tor mit Kette) -> Loch im Zaun finden -> durch
//  5. Den Tower hinaufsteigen
//  6. Kabine: 3 Dinge untersuchen
//  7. "Ich sollte hier weg" -> runtergehen -> TowerPushSequence
// =====================================================
public class WatchtowerStory : MonoBehaviour
{
    [Header("Objekte")]
    public CarController car;
    public GameObject player;
    public Transform redCar;
    public Transform tower;
    public TowerPushSequence push;

    [Header("Zaun (Welt-Koordinaten)")]
    public Vector2 fenceMin = new Vector2(-10f, -9f);   // x / z
    public Vector2 fenceMax = new Vector2(10f, 11f);

    [Header("UI")]
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI taskText;
    public TextMeshProUGUI promptText;
    public CanvasGroup blackScreen;

    [Header("Stimme")]
    public AudioSource playerVoiceAudioSource;
    public AudioClip playerTalking;

    [Header("Timing")]
    public float startSpeed = 13f;
    public float startFadeDuration = 2.5f;
    public float spotRedCarDistance = 70f;
    public float stopNearRedCarDistance = 25f;
    public float textSpeed = 0.045f;
    public float dialogueEndDelay = 1.5f;

    [Header("Texte (Spieler)")]
    public string[] linesDriving = { "Almost there. Just a few more miles." };
    public string[] linesSpotRedCar = { "Wait... isn't that the red car from the gas station?", "Why would anyone park out here in the middle of the night?" };
    public string[] linesAfterExit = { "Hello? Anyone here?", "...The hood is still warm. Where did the driver go?" };
    public string[] linesAtFence = { "The gate is chained shut.", "There has to be another way in." };
    public string[] linesInsideFence = { "A watchtower... and there's a light up there.", "Maybe the driver went up." };
    public string[] linesOnTop = { "Hello? Is anybody up here?", "...Nobody. But someone was here." };
    public string[] linesDoneExploring = { "I shouldn't be here.", "I need to get out of here. Now." };

    [Header("Aufgaben")]
    public string taskStopAtCar = "Am roten Auto anhalten";
    public string taskFindDriver = "Nachsehen, wo der Fahrer ist";
    public string taskFindWayIn = "Einen Weg durch den Zaun finden";
    public string taskClimb = "Den Tower hinaufsteigen";
    public string taskExplore = "Die Kabine untersuchen";
    public string taskLeave = "Den Tower verlassen";
    public int itemsToInspect = 3;

    [Header("Test")]
    [Tooltip("Zum Testen: startet direkt vor dem Loch im Zaun (ohne Fahrt)")]
    public bool testStartAtFence = false;
    public Transform fenceHole;

    public bool CanInspect { get; private set; }

    private int inspected;
    private bool talking;
    private float playerPivotHeight = 0.54f;

    void Start()
    {
        if (player != null)
        {
            if (GroundY(player.transform.position, out float gy)) playerPivotHeight = player.transform.position.y - gy;
            player.SetActive(false);
        }
        Hide(dialogueText); Hide(taskText); Hide(promptText);
        if (blackScreen != null) { blackScreen.gameObject.SetActive(true); blackScreen.alpha = 1f; }

        car.controlsEnabled = false;
        car.engineRunning = true;
        car.LowFuelWarning = false;
        car.fuel = 0.95f;
        if (car.driverCamera != null) car.driverCamera.gameObject.SetActive(true);

        StartCoroutine(Sequence());
    }

    // =================================
    // ABLAUF
    // =================================
    private IEnumerator Sequence()
    {
        if (testStartAtFence) yield return TestStart();
        else yield return Drive();
        yield return OnFoot();
    }

    // Test: ohne Fahrt direkt vor dem Zaun starten
    private IEnumerator TestStart()
    {
        car.controlsEnabled = false;
        car.engineRunning = false;
        car.StopImmediately();
        car.transform.SetPositionAndRotation(new Vector3(2f, car.transform.position.y, 25.8f), Quaternion.Euler(0f, 90f, 0f));
        if (car.driverCamera != null) car.driverCamera.gameObject.SetActive(false);
        Vector3 p = (fenceHole != null ? fenceHole.position : redCar.position) + new Vector3(0f, 0f, 3f);
        if (GroundY(p, out float gy)) p.y = gy;
        p.y += playerPivotHeight;
        player.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, 180f, 0f));
        player.SetActive(true);
        yield return Fade(1f, 0f, 1f);
    }

    private IEnumerator Drive()
    {
        yield return new WaitForSeconds(0.5f);
        car.SetSpeed(startSpeed);
        yield return Fade(1f, 0f, startFadeDuration);
        car.controlsEnabled = true;

        yield return new WaitForSeconds(2f);
        yield return Say(linesDriving);

        // rotes Auto entdecken
        while (Flat(car.transform.position, redCar.position) > spotRedCarDistance) yield return null;
        ShowTask(taskStopAtCar);
        yield return Say(linesSpotRedCar);

        // anhalten + aussteigen
        bool exited = false;
        while (!exited)
        {
            bool stopped = Mathf.Abs(car.CurrentSpeed) < 0.5f;
            bool near = Flat(car.transform.position, redCar.position) < stopNearRedCarDistance;
            if (stopped && near)
            {
                ShowPrompt(InputBindings.FormatPrompt("[E] Aussteigen"));
                if (InputBindings.GetDown(GameAction.Interact))
                {
                    Hide(promptText);
                    yield return ExitCar();
                    exited = true;
                }
            }
            else if (stopped && Flat(car.transform.position, redCar.position) < stopNearRedCarDistance * 2.5f)
                ShowPrompt("Näher an das rote Auto fahren");
            else Hide(promptText);
            yield return null;
        }

    }

    private IEnumerator OnFoot()
    {
        ShowTask(taskFindDriver);
        yield return new WaitForSeconds(0.6f);
        yield return Say(linesAfterExit);

        // Zaun erreichen (oder gleich durchs Loch)
        while (!NearFence() && !InsideFence()) yield return null;
        if (!InsideFence())
        {
            ShowTask(taskFindWayIn);
            yield return Say(linesAtFence);
        }

        // durch das Loch
        while (!InsideFence()) yield return null;
        ShowTask(taskClimb);
        yield return Say(linesInsideFence);

        // oben angekommen
        while (player.transform.position.y < tower.position.y + 8.4f) yield return null;
        CanInspect = true;
        UpdateExploreTask();
        yield return Say(linesOnTop);
    }

    public void OnInspected(InspectItem item)
    {
        StartCoroutine(InspectRoutine(item));
    }

    private IEnumerator InspectRoutine(InspectItem item)
    {
        yield return Say(item.lines);
        inspected++;
        if (inspected < itemsToInspect)
        {
            UpdateExploreTask();
            yield break;
        }
        CanInspect = false;
        yield return new WaitForSeconds(0.5f);
        yield return Say(linesDoneExploring);
        ShowTask(taskLeave);
        if (push != null) push.Arm();
    }

    private void UpdateExploreTask() => ShowTask(taskExplore + " (" + inspected + "/" + itemsToInspect + ")");

    private bool InsideFence()
    {
        Vector3 p = player.transform.position;
        return p.x > fenceMin.x + 0.3f && p.x < fenceMax.x - 0.3f && p.z > fenceMin.y + 0.3f && p.z < fenceMax.y - 0.3f;
    }

    private bool NearFence()
    {
        Vector3 p = player.transform.position;
        return p.x > fenceMin.x - 3f && p.x < fenceMax.x + 3f && p.z > fenceMin.y - 3f && p.z < fenceMax.y + 3f;
    }

    private static float Flat(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }

    // =================================
    // AUSSTEIGEN (wie an der Tankstelle)
    // =================================
    private IEnumerator ExitCar()
    {
        car.controlsEnabled = false;
        car.StopImmediately();
        yield return Fade(0f, 1f, 0.6f);
        car.engineRunning = false;

        Vector3 c = car.transform.position;
        float halfWidth = 1.0f, halfLength = 2.6f;
        BoxCollider best = null;
        foreach (var b in car.GetComponents<BoxCollider>())
            if (!b.isTrigger && (best == null || b.size.x * b.size.z > best.size.x * best.size.z)) best = b;
        if (best != null)
        {
            Vector3 wc = car.transform.TransformPoint(best.center);
            c = new Vector3(wc.x, c.y, wc.z);
            halfWidth = best.size.x * car.transform.lossyScale.x * 0.5f;
            halfLength = best.size.z * car.transform.lossyScale.z * 0.5f;
        }
        const float margin = 0.6f;
        Vector3[] candidates =
        {
            c - car.transform.right * (halfWidth + margin),
            c + car.transform.right * (halfWidth + margin),
            c - car.transform.forward * (halfLength + margin),
            c + car.transform.forward * (halfLength + margin)
        };
        Vector3 exitPos = candidates[0];
        foreach (var p in candidates)
            if (!Physics.CheckCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.7f, 0.35f, ~0, QueryTriggerInteraction.Ignore)) { exitPos = p; break; }
        if (GroundY(exitPos, out float ey)) exitPos.y = ey;
        exitPos.y += playerPivotHeight;

        if (car.driverCamera != null) car.driverCamera.gameObject.SetActive(false);
        // Blick Richtung rotes Auto / Tower
        Vector3 look = redCar.position - exitPos; look.y = 0f;
        player.transform.SetPositionAndRotation(exitPos, look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.Euler(0f, car.transform.eulerAngles.y, 0f));
        player.SetActive(true);

        yield return new WaitForSeconds(0.3f);
        yield return Fade(1f, 0f, 1f);
    }

    // =================================
    // TEXT / UI
    // =================================
    public IEnumerator Say(string[] lines)
    {
        while (talking) yield return null;
        talking = true;
        foreach (var l in lines) if (!string.IsNullOrEmpty(l)) yield return SayLine(l);
        talking = false;
    }

    private IEnumerator SayLine(string message)
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
        for (int i = 1; i <= message.Length; i++) { dialogueText.maxVisibleCharacters = i; yield return wait; }
        dialogueText.maxVisibleCharacters = int.MaxValue;
        if (playerVoiceAudioSource != null) playerVoiceAudioSource.Stop();
        yield return new WaitForSeconds(dialogueEndDelay + message.Length * 0.025f);
        dialogueText.gameObject.SetActive(false);
    }

    public void ShowTask(string text)
    {
        if (taskText == null) return;
        taskText.text = "Aufgabe:\n<size=80%>" + text + "</size>";
        taskText.gameObject.SetActive(true);
    }

    public void HideTask() => Hide(taskText);

    private void ShowPrompt(string text)
    {
        if (promptText == null) return;
        if (!promptText.gameObject.activeSelf || promptText.text != text)
        {
            promptText.text = text;
            promptText.gameObject.SetActive(true);
        }
    }

    private static void Hide(TextMeshProUGUI t) { if (t != null) t.gameObject.SetActive(false); }

    public IEnumerator Fade(float from, float to, float duration)
    {
        if (blackScreen == null) yield break;
        blackScreen.gameObject.SetActive(true);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            blackScreen.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        blackScreen.alpha = to;
        if (to <= 0f) blackScreen.gameObject.SetActive(false);
    }

    private bool GroundY(Vector3 pos, out float y)
    {
        y = 0f; bool found = false; float best = float.MaxValue;
        foreach (var h in Physics.RaycastAll(pos + Vector3.up * 2.5f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (player != null && h.collider.transform.IsChildOf(player.transform)) continue;
            if (car != null && h.collider.transform.IsChildOf(car.transform)) continue;
            if (h.distance < best) { best = h.distance; y = h.point.y; found = true; }
        }
        return found;
    }
}
