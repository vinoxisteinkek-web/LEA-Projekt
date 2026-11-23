using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// =====================================================
// AUTO FÄHRT EINEN FESTEN WEG AB (ohne Spieler)
// Für Autos, die in einer Szene vorbeifahren, z. B. das rote Auto,
// das nach dem Tanken von der Tankstelle wegfährt.
//   - Wegpunkte (Kinder-Objekte oder Liste) der Reihe nach abfahren
//   - Scheinwerfer/Rücklichter gehen beim Losfahren an
//   - Motorgeräusch (3D) läuft mit
//   - Am Ende wird das Auto ausgeblendet (optional)
// Start über Drive() (z. B. aus einem UnityEvent).
// =====================================================
public class ScriptedCarDrive : MonoBehaviour
{
    [Header("Weg")]
    public Transform[] waypoints;
    public float speed = 11f;               // m/s (~40 km/h)
    public float acceleration = 4f;
    public float turnSpeed = 90f;           // Grad pro Sekunde
    public float startDelay = 1.5f;
    [Tooltip("Am Ende ausblenden (Auto ist weg)")]
    public bool hideAtEnd = true;

    [Header("Licht beim Fahren")]
    public Light[] headlights;
    [Tooltip("Renderer, deren Materialien beim Losfahren getauscht werden (z. B. Scheinwerfer aus -> an)")]
    public Renderer[] lightRenderers;
    public Material[] materialsOff;
    public Material[] materialsOn;

    [Header("Sound")]
    public AudioSource engineAudio;
    public float idlePitch = 0.6f;
    public float maxPitch = 1.2f;

    [Header("Danach")]
    public UnityEvent onFinished;

    private bool driving;
    private float currentSpeed;

    public void Drive()
    {
        if (driving || waypoints == null || waypoints.Length == 0) return;
        driving = true;
        StartCoroutine(DriveRoutine());
    }

    private IEnumerator DriveRoutine()
    {
        SetLights(true);
        if (engineAudio != null) { engineAudio.loop = true; engineAudio.pitch = idlePitch; engineAudio.Play(); }
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Vector3 target = waypoints[i].position;
            bool last = i == waypoints.Length - 1;
            while (true)
            {
                Vector3 pos = transform.position;
                Vector3 to = target - pos; to.y = 0f;
                float dist = to.magnitude;
                if (dist < (last ? 0.3f : 2.0f)) break;

                currentSpeed = Mathf.MoveTowards(currentSpeed, speed, acceleration * Time.deltaTime);
                Quaternion want = Quaternion.LookRotation(to.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
                pos += transform.forward * currentSpeed * Time.deltaTime;
                pos.y = GroundY(pos);
                transform.position = pos;

                if (engineAudio != null) engineAudio.pitch = Mathf.Lerp(idlePitch, maxPitch, currentSpeed / Mathf.Max(0.1f, speed));
                yield return null;
            }
        }

        driving = false;
        if (engineAudio != null) engineAudio.Stop();
        if (hideAtEnd) gameObject.SetActive(false);
        onFinished?.Invoke();
    }

    private void SetLights(bool on)
    {
        foreach (var l in headlights) if (l != null) l.enabled = on;
        if (lightRenderers == null) return;
        foreach (var r in lightRenderers)
        {
            if (r == null) continue;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                for (int k = 0; k < materialsOff.Length && k < materialsOn.Length; k++)
                {
                    if (on && mats[i] == materialsOff[k]) mats[i] = materialsOn[k];
                    else if (!on && mats[i] == materialsOn[k]) mats[i] = materialsOff[k];
                }
            r.sharedMaterials = mats;
        }
    }

    // Bodenhöhe unter dem Auto (ignoriert das Auto selbst)
    private float GroundY(Vector3 pos)
    {
        float best = float.MaxValue, y = pos.y;
        foreach (var h in Physics.RaycastAll(pos + Vector3.up * 3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.rigidbody != null) continue; // Spieler / Spielerauto nicht als Boden nehmen
            if (h.distance < best) { best = h.distance; y = h.point.y; }
        }
        return y;
    }
}
