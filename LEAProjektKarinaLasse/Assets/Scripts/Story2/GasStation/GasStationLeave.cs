using System.Collections;
using UnityEngine;

// =====================================================
// TANKSTELLE - WEITERFAHREN
// Nach dem Bezahlen:
//   1. Aufgabe "Zurück zum Auto"
//   2. Am Auto [E] Einsteigen (CarEnterInteractable)
//   3. Losfahren - sobald man weit genug weg ist: abblenden
//      und die nächste Szene laden (Watchtower)
// =====================================================
public class GasStationLeave : MonoBehaviour
{
    [Header("Referenzen")]
    public GasStationPayFlow flow;
    public GasStationIntro intro;
    public CarController car;
    public GameObject player;

    [Header("Texte")]
    public string taskBackToCar = "Zurück zum Auto";
    public string taskDrive = "Weiterfahren";
    public string lineInCar = "Finally. Let's get out of here.";

    [Header("Weiter")]
    [Tooltip("So weit (Meter) muss man von der Tankstelle wegfahren")]
    public float leaveDistance = 70f;
    public Transform gasStation;
    public string nextScene = "WatchtowerScene";
    public float fadeDuration = 2f;
    [Tooltip("Optional: Abfahrt-Sequenz. Wenn gesetzt, startet sie nach dem Einsteigen und lädt am Ende die nächste Szene.")]
    public GasStationDeparture departure;

    public bool CanEnterCar => readyToLeave && !inCar;

    private bool readyToLeave, inCar, loading;

    void Start()
    {
        StartCoroutine(WaitForPaid());
    }

    private IEnumerator WaitForPaid()
    {
        while (flow == null || flow.stage != GasStationPayFlow.Stage.Done) yield return null;
        yield return new WaitForSeconds(3f); // Kassierer-Satz ausreden lassen
        readyToLeave = true;
        if (intro != null) intro.ShowTask(taskBackToCar);
    }

    public void EnterCar()
    {
        if (!CanEnterCar) return;
        inCar = true;
        StartCoroutine(EnterRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        yield return Fade(0f, 1f, 0.6f);

        if (player != null) player.SetActive(false);
        if (car.driverCamera != null) car.driverCamera.gameObject.SetActive(true);
        car.engineRunning = true;
        car.LowFuelWarning = false;
        car.controlsEnabled = true;
        if (intro != null) intro.ShowTask(taskDrive);

        yield return new WaitForSeconds(0.3f);
        yield return Fade(1f, 0f, 1f);
        if (intro != null && !string.IsNullOrEmpty(lineInCar)) StartCoroutine(intro.PlayerSay(lineInCar));

        // Abfahrt-Sequenz (rotes Auto überholt) übernimmt ab hier
        if (departure != null) { departure.Begin(); yield break; }

        Vector3 station = gasStation != null ? gasStation.position : Vector3.zero;
        while (true)
        {
            Vector3 a = car.transform.position, b = station; a.y = b.y = 0f;
            if (Vector3.Distance(a, b) > leaveDistance) break;
            yield return null;
        }

        if (loading) yield break;
        loading = true;
        if (intro != null) intro.HideTask();
        yield return Fade(0f, 1f, fadeDuration);
        car.controlsEnabled = false;
        SceneLoader.Load(nextScene);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        var bs = intro != null ? intro.blackScreen : null;
        if (bs == null) yield break;
        bs.gameObject.SetActive(true);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            bs.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        bs.alpha = to;
        if (to <= 0f) bs.gameObject.SetActive(false);
    }
}
