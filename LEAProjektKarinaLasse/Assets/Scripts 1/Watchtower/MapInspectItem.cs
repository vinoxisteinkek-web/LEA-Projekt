using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// KARTE AUF DEM TISCH (Watchtower-Kabine)
// [E] Karte ansehen -> die Karte wird groß im Bild angezeigt
// (typisches Inspizieren). Der Spieler steht still.
//   Mausrad           = heranzoomen
//   Maus bewegen      = (gezoomt) über die Karte schauen
//   [E] / Rechtsklick = schließen
// Beim ersten Schließen zählt die Karte als untersucht und
// der Spieler sagt die Sätze aus 'lines'.
// Die Anzeige (Canvas) baut das Script selbst.
// =====================================================
public class MapInspectItem : InspectItem
{
    [Header("Karte")]
    public Texture mapTexture;
    [Tooltip("Seitenverhältnis der Karte (Breite / Höhe)")]
    public float mapAspect = 4f / 3f;
    [Tooltip("Anteil der Bildschirmhöhe, den die Karte einnimmt")]
    [Range(0.5f, 1f)] public float screenHeight = 0.9f;
    public float maxZoom = 3f;
    public float zoomSpeed = 0.25f;
    public float panSpeed = 0.5f;
    public AudioClip openSound;   // Papier-Rascheln (optional)
    public AudioClip closeSound;
    public string hint = "Mausrad - Zoom      Maus - Umsehen      [E] - Schließen";

    [Header("Spieler (leer = automatisch suchen)")]
    public GameObject player;

    public bool IsOpen { get; private set; }

    private Canvas canvas;
    private CanvasGroup group;
    private RectTransform mapRect;
    private float zoom = 1f;
    private Vector2 pan;          // -1..1
    private readonly List<Behaviour> frozen = new List<Behaviour>();
    private AudioSource audioSrc;

    void Reset() { promptText = "[E] Karte ansehen"; }

    // die Karte darf man sich mehrmals ansehen
    public override bool CanInteract(GameObject p) => !IsOpen && story != null && story.CanInspect;

    public override void Interact(GameObject p)
    {
        if (IsOpen) return;
        if (p != null) player = p;
        StartCoroutine(OpenRoutine());
    }

    private bool closeRequested;

    // Karte von außen schließen (z. B. aus einem anderen Script)
    public void Close() { if (IsOpen) closeRequested = true; }

    private IEnumerator OpenRoutine()
    {
        IsOpen = true;
        closeRequested = false;
        BuildUI();
        Freeze(true);
        zoom = 1f; pan = Vector2.zero;
        canvas.gameObject.SetActive(true);
        ApplyView();
        Play(openSound);

        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.2f;
            group.alpha = k;
            mapRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
            yield return null;
        }
        group.alpha = 1f; mapRect.localScale = Vector3.one;

        yield return null; // die E-Taste vom Öffnen nicht gleich als Schließen werten
        while (true)
        {
            if (Time.timeScale > 0f) // nicht im Pausemenü
            {
                if (closeRequested || InputBindings.GetDown(GameAction.Interact) || Input.GetMouseButtonDown(1)) break;

                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f)
                    zoom = Mathf.Clamp(zoom * (1f + wheel * zoomSpeed), 1f, maxZoom);
                if (zoom > 1.01f)
                {
                    // Maus bewegen = über die Karte schauen
                    pan.x = Mathf.Clamp(pan.x - Input.GetAxis("Mouse X") * panSpeed * 0.05f, -1f, 1f);
                    pan.y = Mathf.Clamp(pan.y - Input.GetAxis("Mouse Y") * panSpeed * 0.05f, -1f, 1f);
                }
                else pan = Vector2.Lerp(pan, Vector2.zero, 10f * Time.unscaledDeltaTime);
                ApplyView();
            }
            yield return null;
        }

        Play(closeSound);
        for (float t = 0f; t < 0.15f; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / 0.15f;
            yield return null;
        }
        canvas.gameObject.SetActive(false);
        Freeze(false);
        IsOpen = false;

        MarkInspected(); // nur beim ersten Mal: zählt + Sätze
    }

    private void ApplyView()
    {
        if (mapRect == null) return;
        var parent = (RectTransform)mapRect.parent;
        float h = parent.rect.height * screenHeight;
        float w = h * mapAspect;
        if (w > parent.rect.width * 0.96f) { w = parent.rect.width * 0.96f; h = w / mapAspect; }
        mapRect.sizeDelta = new Vector2(w, h) * zoom;
        float maxX = (w * zoom - w) * 0.5f, maxY = (h * zoom - h) * 0.5f;
        mapRect.anchoredPosition = new Vector2(pan.x * maxX, pan.y * maxY);
    }

    private void Freeze(bool on)
    {
        if (player == null)
        {
            var pi = FindFirstObjectByType<PlayerInteractor>();
            if (pi != null) player = pi.gameObject;
        }
        if (on)
        {
            frozen.Clear();
            if (player == null) return;
            foreach (var b in player.GetComponentsInChildren<Behaviour>())
            {
                if (b == null || !b.enabled) continue;
                if (b is PlayerController || b is CameraController || b is PlayerInteractor || b is PickupItem)
                {
                    b.enabled = false;
                    frozen.Add(b);
                }
            }
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
        }
        else
        {
            foreach (var b in frozen) if (b != null) b.enabled = true;
            frozen.Clear();
        }
    }

    private void Play(AudioClip c)
    {
        if (c == null) return;
        if (audioSrc == null) { audioSrc = gameObject.AddComponent<AudioSource>(); audioSrc.playOnAwake = false; audioSrc.spatialBlend = 0f; }
        audioSrc.PlayOneShot(c);
    }

    private static RectTransform Stretch(GameObject g, Transform parent)
    {
        g.transform.SetParent(parent, false);
        var r = (RectTransform)g.transform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }

    private void BuildUI()
    {
        if (canvas != null) return;
        var go = new GameObject("MapInspectCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        group = go.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        // abgedunkelter Hintergrund
        var bg = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        Stretch(bg, go.transform);
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

        // Bereich, der beim Zoomen abschneidet
        var view = new GameObject("View", typeof(RectTransform), typeof(RectMask2D));
        Stretch(view, go.transform);

        var map = new GameObject("Map", typeof(RectTransform), typeof(RawImage), typeof(Shadow));
        map.transform.SetParent(view.transform, false);
        mapRect = (RectTransform)map.transform;
        mapRect.anchorMin = mapRect.anchorMax = mapRect.pivot = new Vector2(0.5f, 0.5f);
        var raw = map.GetComponent<RawImage>();
        raw.texture = mapTexture;
        raw.color = new Color(0.93f, 0.9f, 0.84f); // etwas dunkler, wie bei Lampenlicht
        raw.raycastTarget = false;
        var sh = map.GetComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.6f);
        sh.effectDistance = new Vector2(10f, -12f);

        // Hinweis unten
        var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGo.transform.SetParent(go.transform, false);
        var hr = (RectTransform)hintGo.transform;
        hr.anchorMin = new Vector2(0f, 0f); hr.anchorMax = new Vector2(1f, 0f);
        hr.pivot = new Vector2(0.5f, 0f);
        hr.anchoredPosition = new Vector2(0f, 10f); hr.sizeDelta = new Vector2(0f, 36f);
        var tmp = hintGo.GetComponent<TextMeshProUGUI>();
        tmp.text = InputBindings.FormatPrompt(hint);
        tmp.fontSize = 22; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0.75f);
        tmp.raycastTarget = false;

        go.SetActive(false);
    }

    void OnDisable()
    {
        if (IsOpen) { Freeze(false); IsOpen = false; }
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
