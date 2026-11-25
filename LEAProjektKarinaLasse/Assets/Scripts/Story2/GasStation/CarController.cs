using UnityEngine;

// =====================================================
// AUTO
// Einfache Arcade-Steuerung aus der Fahrerperspektive.
// W/S = Gas/Bremse (bzw. rückwärts), A/D = Lenken, Leertaste = Handbremse.
// Das Auto klebt per Raycast am Boden (kein Springen, kein Umkippen),
// Kollisionen mit Wänden/Bäumen/Zapfsäulen macht die Physik.
// Räder, Lenkrad, Tacho- und Tanknadel werden automatisch gesucht
// (Objektnamen aus Car.fbx) und mitgedreht.
// =====================================================
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Fahren")]
    public float maxSpeed = 16f;            // m/s (~58 km/h)
    public float reverseMaxSpeed = 4f;
    public float acceleration = 5f;
    public float brakeForce = 12f;
    public float rollDrag = 1.5f;           // Ausrollen ohne Gas
    public float turnSpeed = 55f;           // Grad pro Sekunde bei voller Lenkung
    public float maxSteerAngle = 30f;       // Einschlag der Vorderräder (nur Optik)
    public float steerResponse = 4f;

    [Header("Boden")]
    public float groundOffset = 0f;         // Pivot des Autos liegt auf Reifenunterkante
    public float groundFollowSpeed = 12f;

    [Header("Teile (leer lassen = automatisch suchen)")]
    public Transform wheelFL;
    public Transform wheelFR;
    public Transform wheelRL;
    public Transform wheelRR;
    public Transform steeringWheel;
    public Transform fuelNeedle;
    public Transform speedoNeedle;
    public Renderer fuelWarningLight;
    public Material fuelWarningOn;
    public Material fuelWarningOff;
    public float wheelRadius = 0.32f;
    public bool invertSteeringWheel = false;
    public bool invertNeedles = false;

    [Header("Tank")]
    [Range(0f, 1f)] public float fuel = 0.16f;
    public float fuelPerMeter = 0.00025f;
    public float minFuel = 0.04f;           // Tank wird nie ganz leer (kein Liegenbleiben)
    public float fuelNeedleEmptyAngle = -60f;
    public float fuelNeedleFullAngle = 60f;
    public float speedoZeroAngle = -120f;
    public float speedoMaxAngle = 120f;
    public float blinkInterval = 0.5f;

    [Header("Kamera")]
    public Camera driverCamera;
    public float lookYawLimit = 100f;
    public float lookPitchUp = 50f;
    public float lookPitchDown = 45f;

    [Header("Sound")]
    public AudioSource engineAudio;
    public float idlePitch = 0.55f;
    public float maxPitch = 1.15f;
    public float idleVolume = 0.25f;
    public float maxVolume = 0.5f;

    // Wird vom Story-Skript gesteuert
    public bool controlsEnabled = true;
    public bool engineRunning = true;
    public bool LowFuelWarning { get; set; }

    public float CurrentSpeed => speed;                 // m/s, negativ = rückwärts
    public float CurrentSpeedKmh => Mathf.Abs(speed) * 3.6f;

    private Rigidbody rb;
    private Collider[] ownColliders;
    private float speed;
    private float lastAppliedSpeed;
    private float steerInput;
    private float throttleInput;
    private bool handbrake;
    private float wheelSpin;
    private float lookYaw, lookPitch;
    private Quaternion cameraBaseRot;

    // Drehachsen der Teile (lokal), automatisch aus dem Mesh bestimmt
    private struct Part
    {
        public Transform t;
        public Quaternion baseRot;
        public Vector3 axis;
        public Vector3 steerAxis;
    }
    private Part pFL, pFR, pRL, pRR, pSteer, pFuel, pSpeedo;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ownColliders = GetComponentsInChildren<Collider>(true);

        if (wheelFL == null) wheelFL = FindDeep("Wheel_FL");
        if (wheelFR == null) wheelFR = FindDeep("Wheel_FR");
        if (wheelRL == null) wheelRL = FindDeep("Wheel_RL");
        if (wheelRR == null) wheelRR = FindDeep("Wheel_RR");
        if (steeringWheel == null) steeringWheel = FindDeep("SteeringWheel");
        if (fuelNeedle == null) fuelNeedle = FindDeep("FuelGauge_Needle");
        if (speedoNeedle == null) speedoNeedle = FindDeep("Speedo_Needle");
        if (fuelWarningLight == null)
        {
            Transform w = FindDeep("FuelWarning_Light");
            if (w != null) fuelWarningLight = w.GetComponent<Renderer>();
        }

        pFL = MakePart(wheelFL);
        pFR = MakePart(wheelFR);
        pRL = MakePart(wheelRL);
        pRR = MakePart(wheelRR);
        pSteer = MakePart(steeringWheel);
        pFuel = MakePart(fuelNeedle);
        pSpeedo = MakePart(speedoNeedle);

        if (driverCamera != null)
            cameraBaseRot = driverCamera.transform.localRotation;
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // =================================
    // EINGABE + OPTIK
    // =================================
    void Update()
    {
        if (controlsEnabled && Time.timeScale > 0f)
        {
            throttleInput = InputBindings.GetVertical();
            steerInput = Mathf.MoveTowards(steerInput, InputBindings.GetHorizontal(), steerResponse * Time.deltaTime);
            handbrake = InputBindings.Get(GameAction.Handbrake);
        }
        else
        {
            throttleInput = 0f;
            steerInput = Mathf.MoveTowards(steerInput, 0f, steerResponse * Time.deltaTime);
            handbrake = false; // ausrollen lassen (z. B. während des Einblendens)
        }

        UpdateLook();
        UpdateVisuals();
        UpdateEngineSound();
    }

    // =================================
    // PHYSIK
    // =================================
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 fwd = transform.forward;

        // Gegen etwas gefahren? Dann Geschwindigkeit an die echte anpassen
        float measured = Vector3.Dot(rb.linearVelocity, fwd);
        if (Mathf.Abs(measured) < Mathf.Abs(lastAppliedSpeed) - 1f)
            speed = measured;

        // Gas / Bremse / Rückwärts
        if (handbrake)
        {
            speed = Mathf.MoveTowards(speed, 0f, brakeForce * dt);
        }
        else if (throttleInput > 0.01f)
        {
            if (speed < -0.1f) speed = Mathf.MoveTowards(speed, 0f, brakeForce * dt);
            else speed = Mathf.MoveTowards(speed, maxSpeed, acceleration * throttleInput * dt);
        }
        else if (throttleInput < -0.01f)
        {
            if (speed > 0.1f) speed = Mathf.MoveTowards(speed, 0f, brakeForce * dt);
            else speed = Mathf.MoveTowards(speed, -reverseMaxSpeed, acceleration * 0.6f * dt);
        }
        else
        {
            speed = Mathf.MoveTowards(speed, 0f, rollDrag * dt);
        }

        // Lenken (nur wenn das Auto rollt)
        float speedFactor = Mathf.Clamp01(Mathf.Abs(speed) / 4f);
        float yaw = steerInput * turnSpeed * speedFactor * Mathf.Sign(speed) * dt;
        if (Mathf.Abs(yaw) > 0.0001f)
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));

        // Am Boden halten
        float vy = 0f;
        if (FindGround(out float groundY))
            vy = Mathf.Clamp((groundY + groundOffset - rb.position.y) * groundFollowSpeed, -8f, 8f);

        rb.linearVelocity = fwd * speed + Vector3.up * vy;
        lastAppliedSpeed = speed;

        // Tank
        float dist = Mathf.Abs(speed) * dt;
        fuel = Mathf.Max(minFuel, fuel - dist * fuelPerMeter);
    }

    private bool FindGround(out float groundY)
    {
        groundY = 0f;
        Vector3 origin = rb.position + Vector3.up * 2f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        bool found = false;
        foreach (var h in hits)
        {
            if (IsOwn(h.collider)) continue;
            if (h.distance < best)
            {
                best = h.distance;
                groundY = h.point.y;
                found = true;
            }
        }
        return found;
    }

    private bool IsOwn(Collider c)
    {
        foreach (var o in ownColliders)
            if (o == c) return true;
        return false;
    }

    // =================================
    // UMSCHAUEN IM AUTO
    // =================================
    private void UpdateLook()
    {
        if (driverCamera == null || !driverCamera.isActiveAndEnabled || Time.timeScale == 0f)
            return;

        float sens = GameSettings.Sensitivity * 0.01f;
        lookYaw = Mathf.Clamp(lookYaw + Input.GetAxisRaw("Mouse X") * sens, -lookYawLimit, lookYawLimit);
        lookPitch = Mathf.Clamp(lookPitch - Input.GetAxisRaw("Mouse Y") * sens, -lookPitchUp, lookPitchDown);
        driverCamera.transform.localRotation = cameraBaseRot * Quaternion.Euler(lookPitch, lookYaw, 0f);
    }

    // =================================
    // RÄDER, LENKRAD, NADELN, WARNLAMPE
    // =================================
    private void UpdateVisuals()
    {
        wheelSpin += speed * Time.deltaTime / wheelRadius * Mathf.Rad2Deg;
        wheelSpin %= 360f;
        float steerAngle = steerInput * maxSteerAngle;

        ApplyWheel(pFL, steerAngle);
        ApplyWheel(pFR, steerAngle);
        ApplyWheel(pRL, 0f);
        ApplyWheel(pRR, 0f);

        float sw = steerInput * 120f * (invertSteeringWheel ? -1f : 1f);
        ApplyRotation(pSteer, sw);

        float n = invertNeedles ? -1f : 1f;
        ApplyRotation(pFuel, Mathf.Lerp(fuelNeedleEmptyAngle, fuelNeedleFullAngle, fuel) * n);
        float sp = Mathf.Clamp01(Mathf.Abs(speed) / 33f); // Skala bis ~120 km/h
        ApplyRotation(pSpeedo, Mathf.Lerp(speedoZeroAngle, speedoMaxAngle, sp) * n);

        if (fuelWarningLight != null && fuelWarningOn != null && fuelWarningOff != null)
        {
            bool on = LowFuelWarning && engineRunning && (Time.time % (blinkInterval * 2f)) < blinkInterval;
            Material target = on ? fuelWarningOn : fuelWarningOff;
            if (fuelWarningLight.sharedMaterial != target)
                fuelWarningLight.sharedMaterial = target;
        }
    }

    private void UpdateEngineSound()
    {
        if (engineAudio == null)
            return;

        if (!engineRunning)
        {
            engineAudio.volume = Mathf.MoveTowards(engineAudio.volume, 0f, Time.deltaTime);
            if (engineAudio.volume <= 0.001f && engineAudio.isPlaying) engineAudio.Stop();
            return;
        }

        if (!engineAudio.isPlaying) engineAudio.Play();
        float t = Mathf.Clamp01(Mathf.Abs(speed) / maxSpeed);
        engineAudio.pitch = Mathf.Lerp(idlePitch, maxPitch, t);
        engineAudio.volume = Mathf.Lerp(idleVolume, maxVolume, Mathf.Max(t, Mathf.Abs(throttleInput) * 0.5f));
    }

    // =================================
    // HILFSFUNKTIONEN
    // =================================
    // Für Zwischensequenzen: Auto rollt mit dieser Geschwindigkeit los (m/s)
    public void SetSpeed(float metersPerSecond)
    {
        speed = metersPerSecond;
    }

    public void StopImmediately()
    {
        speed = 0f;
        lastAppliedSpeed = 0f;
        if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
    }

    private void ApplyWheel(Part p, float steerAngle)
    {
        if (p.t == null) return;
        p.t.localRotation = Quaternion.AngleAxis(steerAngle, p.steerAxis) * p.baseRot * Quaternion.AngleAxis(wheelSpin, p.axis);
    }

    private void ApplyRotation(Part p, float angle)
    {
        if (p.t == null) return;
        p.t.localRotation = p.baseRot * Quaternion.AngleAxis(angle, p.axis);
    }

    private Part MakePart(Transform t)
    {
        var p = new Part { t = t };
        if (t == null) return p;

        p.baseRot = t.localRotation;
        p.axis = Vector3.right;

        // Drehachse = dünnste Achse des Meshes (Radachse, Lenkrad-/Zifferblatt-Normale)
        var mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Vector3 e = mf.sharedMesh.bounds.extents;
            if (e.x <= e.y && e.x <= e.z) p.axis = Vector3.right;
            else if (e.y <= e.x && e.y <= e.z) p.axis = Vector3.up;
            else p.axis = Vector3.forward;
        }

        // Lenkachse (Hochachse des Autos) im Raum des Eltern-Objekts
        p.steerAxis = t.parent != null ? t.parent.InverseTransformDirection(transform.up) : Vector3.up;
        return p;
    }

    private Transform FindDeep(string n)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == n) return t;
        return null;
    }
}
