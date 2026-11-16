using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================
// VHS-OVERLAY (Camcorder-Anzeige)
// =====================================
// Zeigt "PLAY" oben links und Uhrzeit + Datum unten links,
// wie bei einer alten Videokassette. Wird komplett per Code erzeugt,
// einfach auf ein leeres GameObject in der Szene legen.
public class VHSOverlay : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private string topLeftText = "PLAY";
    [SerializeField] private string topRightText = "SP";
    [SerializeField] private string dateText = "NOV. 16 1998";

    [Header("Startzeit der Aufnahme (laeuft im Spiel weiter)")]
    [SerializeField, Range(0, 23)] private int startHour = 23;
    [SerializeField, Range(0, 59)] private int startMinute = 47;
    [SerializeField, Range(0, 59)] private int startSecond = 12;

    [Header("Aussehen")]
    [SerializeField] private float fontSize = 44f;
    [SerializeField] private Color textColor = new Color(0.95f, 0.95f, 0.95f, 0.9f);
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private int sortingOrder = 5; // unter Pausemenue (50) und Ladebildschirm

    private TextMeshProUGUI clockText;
    private TextMeshProUGUI playText;
    private float elapsed;

    private void Awake()
    {
        BuildUI();
    }

    private void Update()
    {
        elapsed += Time.deltaTime; // steht in der Pause still

        int total = startHour * 3600 + startMinute * 60 + startSecond + Mathf.FloorToInt(elapsed);
        int h = (total / 3600) % 24;
        int m = (total / 60) % 60;
        int s = total % 60;

        string ampm = h < 12 ? "AM" : "PM";
        int h12 = h % 12;
        if (h12 == 0) h12 = 12;

        clockText.text = $"{ampm} {h12:00}:{m:00}:{s:00}\n{dateText}";

        // "PLAY" blinkt leicht, wie bei echten Geraeten nach dem Start
        if (playText != null)
            playText.alpha = elapsed < 4f && Mathf.Repeat(elapsed, 1f) > 0.6f ? 0f : textColor.a;
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        playText = MakeText("Play", topLeftText, new Vector2(0, 1), new Vector2(70, -55), TextAlignmentOptions.TopLeft);
        MakeText("Speed", topRightText, new Vector2(1, 1), new Vector2(-70, -55), TextAlignmentOptions.TopRight);
        clockText = MakeText("Clock", "", new Vector2(0, 0), new Vector2(70, 55), TextAlignmentOptions.BottomLeft);
    }

    private TextMeshProUGUI MakeText(string name, string text, Vector2 anchor, Vector2 pos, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.color = textColor;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        t.characterSpacing = 8f;

        // leichter Schatten wie bei eingeblendeter Camcorder-Schrift
        t.fontMaterial.EnableKeyword("UNDERLAY_ON");
        t.fontMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.6f));
        t.fontMaterial.SetFloat("_UnderlayOffsetX", 0.6f);
        t.fontMaterial.SetFloat("_UnderlayOffsetY", -0.6f);

        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(600, 140);
        return t;
    }
}
