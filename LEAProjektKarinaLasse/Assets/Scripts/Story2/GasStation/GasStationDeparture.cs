using System.Collections;
using UnityEngine;

// =====================================================
// TANKSTELLE - ABFAHRT (Sequenz)
// Startet, sobald der Spieler nach dem Bezahlen mit dem Auto
// vom Gelände auf die Straße fährt:
//   1. Spielerauto wird auf der RECHTEN Spur festgehalten und fährt
//      von selbst weiter (Umschauen mit der Maus geht weiter)
//   2. Das rote Auto fährt vom Parkplatz los, kommt von hinten
//      auf der LINKEN Spur und überholt
//   3. Es schert vor dem Spieler wieder ein und fährt davon
//   4. Abblenden -> nächste Szene (Watchtower)
// Die Fahrtrichtung (links/rechts auf der Straße) wird beim
// Auffahren erkannt, die Spuren passen sich an.
// =====================================================
[DefaultExecutionOrder(-10)] // vor dem CarController (FixedUpdate)
public class GasStationDeparture : MonoBehaviour
{
    [Header("Referenzen")]
    public GasStationIntro intro;
    public CarController playerCar;
    public Transform redCar;
    [Tooltip("Für Licht + Material des roten Autos")]
    public ScriptedCarDrive redCarLights;
    public AudioSource redCarEngine;
    [Tooltip("Weg vom Parkplatz Richtung Straße (ohne Straßenpunkt)")]
    public Transform[] redCarExitPath;

    [Header("Straße")]
    public float roadCenterZ = 27.8f;
    public float laneOffset = 2.0f;
    [Tooltip("Ab dieser z-Position gilt das Spielerauto als 'auf der Straße'")]
    public float roadEnterZ = 22.5f;
    [Tooltip("Die Sequenz startet erst, wenn man so weit (m) von der Tankstelle die Straße entlang gefahren ist")]
    public float triggerDistance = 35f;
    public Transform gasStation;
    [Tooltip("Ab dieser Entfernung (m) von der Tankstelle fährt das rote Auto vom Parkplatz los. Kleiner = früher")]
    public float redCarStartDistance = 35f;

    [Header("Spielerauto (gesperrt)")]
    public float playerLockSpeed = 8.5f;
    public float playerAccel = 3f;
    public float lookAhead = 9f;
    public float maxTurnRate = 70f;

    [Header("Rotes Auto")]
    public float lotSpeed = 8f;
    public float turnInSpeed = 6f;
    [Tooltip("Licht/Motor gehen sofort an, losgefahren wird erst nach dieser Zeit (Vorsprung für den Spieler)")]
    public float redCarStartDelay = 1.8f;
    public float overtakeSpeed = 26f;
    public float overtakeAccel = 8f;
    [Tooltip("So weit vor dem Spieler schert es wieder ein (m)")]
    public float mergeAhead = 14f;
    public float mergeDuration = 2.2f;
    public float driveAwaySeconds = 4f;

    [Header("Text / Ende")]
    public string lineOvertake = "Whoa! ...Somebody's in a hurry.";
    public float endDelay = 1.2f;
    public float fadeDuration = 1.6f;
    public string nextScene = "WatchtowerScene";

    public bool Running { get; private set; }

    private bool locked;
    private float dir = 1f, rightZ, leftZ;
    private Rigidbody playerRb;
    private bool redMerged;

    public void Begin()
    {
        if (Running) return;
        Running = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        playerRb = playerCar.GetComponent<Rigidbody>();

        // 1) warten, bis der Spieler auf der Straße ist und triggerDistance weit
        //    von der Tankstelle weg (das rote Auto fährt los, sobald
        //    redCarStartDistance erreicht ist)
        float stationX = gasStation != null ? gasStation.position.x : 0f;
        bool redStarted = false;
        while (true)
        {
            bool onRoad = playerCar.transform.position.z >= roadEnterZ;
            float dist = Mathf.Abs(playerCar.transform.position.x - stationX);
            if (onRoad && !redStarted && dist >= redCarStartDistance)
            {
                redStarted = true;
                StartCoroutine(RedCarRoutine());
            }
            if (onRoad && dist >= triggerDistance) break;
            yield return null;
        }

        // 2) Spieler wird auf der rechten Spur gesperrt, rotes Auto überholt
        float fx = playerCar.transform.forward.x;
        dir = fx < -0.2f ? -1f : 1f;
        rightZ = roadCenterZ - dir * laneOffset;
        leftZ = roadCenterZ + dir * laneOffset;
        playerCar.controlsEnabled = false;
        locked = true;
        if (!redStarted) StartCoroutine(RedCarRoutine());

        while (!redMerged) yield return null;
        yield return new WaitForSeconds(endDelay);

        // 3) Abblenden + nächste Szene
        if (intro != null) intro.HideTask();
        yield return Fade(0f, 1f, fadeDuration);
        SceneLoader.Load(nextScene);
    }

    // Spielerauto auf der rechten Spur halten
    void FixedUpdate()
    {
        if (!locked || playerCar == null) return;
        playerCar.controlsEnabled = false; // falls ein anderes Script sie wieder anschaltet
        float dt = Time.fixedDeltaTime;
        Vector3 pos = playerCar.transform.position;
        Vector3 target = new Vector3(pos.x + dir * lookAhead, pos.y, rightZ);
        Vector3 to = target - pos; to.y = 0f;
        if (to.sqrMagnitude > 0.01f)
        {
            Quaternion want = Quaternion.LookRotation(to.normalized, Vector3.up);
            playerRb.MoveRotation(Quaternion.RotateTowards(playerRb.rotation, want, maxTurnRate * dt));
        }
        float v = Mathf.MoveTowards(playerCar.CurrentSpeed, playerLockSpeed, playerAccel * dt);
        playerCar.SetSpeed(v + playerCar.rollDrag * dt); // Ausrollen im CarController ausgleichen
    }

    // =================================
    // ROTES AUTO
    // =================================
    private IEnumerator RedCarRoutine()
    {
        foreach (var c in redCar.GetComponentsInChildren<Collider>()) c.enabled = false;
        if (redCarLights != null) redCarLights.SetLights(true);
        if (redCarEngine != null) { redCarEngine.loop = true; redCarEngine.Play(); }

        float spd = 0f;
        // Licht + Motor sind schon an, losfahren erst kurz danach -> Spieler hat Vorsprung
        if (redCarStartDelay > 0f) yield return new WaitForSeconds(redCarStartDelay);
        // vom Parkplatz zur Straße
        foreach (var wp in redCarExitPath)
        {
            if (wp == null) continue;
            while (Flat(redCar.position, wp.position) > 2f)
            {
                spd = Mathf.MoveTowards(spd, lotSpeed, 5f * Time.deltaTime);
                MoveRed(wp.position, spd, 110f);
                yield return null;
            }
        }
        // an der Ausfahrt warten, bis die Sequenz weiter unten auf der Straße startet
        while (!locked)
        {
            spd = Mathf.MoveTowards(spd, 0f, 10f * Time.deltaTime);
            if (spd > 0.01f) MoveRed(redCar.position + redCar.forward * 5f, spd, 0f);
            yield return null;
        }
        // auf die linke Spur einbiegen
        float headingErr = 999f;
        float turnStart = Time.time;
        while ((Mathf.Abs(redCar.position.z - leftZ) > 0.4f || headingErr > 8f) && Time.time - turnStart < 6f)
        {
            spd = Mathf.MoveTowards(spd, turnInSpeed, 5f * Time.deltaTime);
            MoveRed(new Vector3(redCar.position.x + dir * 6f, redCar.position.y, leftZ), spd, 120f);
            headingErr = Vector3.Angle(redCar.forward, new Vector3(dir, 0f, 0f));
            yield return null;
        }

        // überholen
        bool said = false;
        while (true)
        {
            spd = Mathf.MoveTowards(spd, overtakeSpeed, overtakeAccel * Time.deltaTime);
            LaneRed(leftZ, spd);
            float ahead = (redCar.position.x - playerCar.transform.position.x) * dir;
            if (!said && ahead > -4f)
            {
                said = true;
                if (intro != null && !string.IsNullOrEmpty(lineOvertake)) StartCoroutine(intro.PlayerSay(lineOvertake));
            }
            if (ahead > mergeAhead) break;
            yield return null;
        }

        // wieder einscheren
        for (float t = 0f; t < mergeDuration; t += Time.deltaTime)
        {
            LaneRed(Mathf.Lerp(leftZ, rightZ, Mathf.SmoothStep(0f, 1f, t / mergeDuration)), spd);
            yield return null;
        }
        redMerged = true;

        // davonfahren
        for (float t = 0f; t < driveAwaySeconds; t += Time.deltaTime)
        {
            LaneRed(rightZ, spd);
            yield return null;
        }
        if (redCarEngine != null) redCarEngine.Stop();
        redCar.gameObject.SetActive(false);
    }

    private void LaneRed(float laneZ, float spd)
    {
        Vector3 target = new Vector3(redCar.position.x + dir * 12f, redCar.position.y, laneZ);
        MoveRed(target, spd, 60f);
    }

    private void MoveRed(Vector3 target, float spd, float turnRate)
    {
        Vector3 pos = redCar.position;
        Vector3 to = target - pos; to.y = 0f;
        if (to.sqrMagnitude > 0.01f)
            redCar.rotation = Quaternion.RotateTowards(redCar.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), turnRate * Time.deltaTime);
        pos += redCar.forward * spd * Time.deltaTime;
        pos.y = GroundY(pos, pos.y);
        redCar.position = pos;
        if (redCarEngine != null) redCarEngine.pitch = Mathf.Lerp(0.6f, 1.3f, spd / overtakeSpeed);
    }

    private float GroundY(Vector3 pos, float fallback)
    {
        float best = float.MaxValue, y = fallback;
        foreach (var h in Physics.RaycastAll(pos + Vector3.up * 3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(redCar)) continue;
            if (h.rigidbody != null) continue;
            if (h.distance < best) { best = h.distance; y = h.point.y; }
        }
        return y;
    }

    private static float Flat(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }

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
    }
}
