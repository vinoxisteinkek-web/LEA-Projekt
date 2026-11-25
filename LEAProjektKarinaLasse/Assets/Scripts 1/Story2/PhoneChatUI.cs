using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// CHAT-FENSTER (wie "Messages" am Mac)
// Wird komplett per Code gebaut. Benutzung:
//   chat.Show("Emilia");
//   chat.AddTimestamp("Today 08:52 PM");
//   chat.AddMessage(false, "Are you already on the way?");   // false = sie, true = ich
//   yield return chat.TypeInput("leaving right now");        // tippt ins Eingabefeld
//   chat.SendInput();                                         // Eingabe als eigene Blase senden
//   chat.SetTyping(true);                                     // "..." von ihr
//   chat.SetHint("[E] Senden");
//   chat.Hide();
// =====================================================
public class PhoneChatUI : MonoBehaviour
{
    [Header("Sprites (abgerundete Ecken)")]
    public Sprite bubbleSprite;
    public Sprite windowSprite;
    public Sprite circleSprite;

    [Header("Sounds (optional)")]
    public AudioClip receiveSound;
    public AudioClip sendSound;

    [Header("Farben")]
    public Color windowColor = new Color(0.95f, 0.95f, 0.95f);
    public Color panelColor = Color.white;
    public Color myBubble = new Color(0.04f, 0.52f, 1f);
    public Color theirBubble = new Color(0.91f, 0.91f, 0.92f);
    public Color theirText = new Color(0.2f, 0.2f, 0.22f);
    public Color timeColor = new Color(0.45f, 0.45f, 0.47f);

    public bool IsOpen { get; private set; }

    private CursorLockMode prevLock = CursorLockMode.Locked;
    private bool prevVisible = false;

    // Maße (Referenz 1920x1080)
    const float WinW = 600f, WinH = 800f;
    const float BubbleMaxText = 330f, PadX = 18f, PadY = 11f, SideMargin = 24f, FontSize = 21f;

    private Canvas canvas;
    private CanvasGroup group;
    private RectTransform window, content, viewport;
    private TextMeshProUGUI titleText, inputText, hintText;
    private RectTransform typingRow;
    private readonly List<RectTransform> rows = new List<RectTransform>();
    private readonly List<float> rowSpacing = new List<float>();
    private int lastSender = -1; // -1 none, 0 them, 1 me, 2 time
    private AudioSource audioSrc;
    private string pendingInput = "";

    // ------------------------------------------------------------
    public void Show(string contact)
    {
        Build();
        titleText.text = "Messages  <size=65%><voffset=0.12em>></voffset></size>  " + contact;
        canvas.gameObject.SetActive(true);
        IsOpen = true;
        // Maus freigeben, damit man das Fenster ziehen kann
        prevLock = Cursor.lockState; prevVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        group.blocksRaycasts = true;
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        if (!IsOpen) return;
        IsOpen = false;
        group.blocksRaycasts = false;
        Cursor.lockState = prevLock; Cursor.visible = prevVisible;
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    public void Clear()
    {
        Build();
        foreach (var r in rows) if (r != null) Destroy(r.gameObject);
        rows.Clear(); rowSpacing.Clear(); lastSender = -1;
        SetTyping(false);
        Relayout();
    }

    public void AddTimestamp(string text)
    {
        Build();
        var row = NewRow("Time", 34f);
        var t = NewText(row, text, 15f, timeColor, TextAlignmentOptions.Center);
        var tr = t.rectTransform;
        tr.anchorMin = new Vector2(0f, 0f); tr.anchorMax = new Vector2(1f, 1f); tr.offsetMin = tr.offsetMax = Vector2.zero;
        AddRow(row, lastSender == -1 ? 4f : 14f);
        lastSender = 2;
    }

    // mine = true -> blaue Blase rechts
    public void AddMessage(bool mine, string text, bool playSound = false)
    {
        Build();
        bool hadTyping = typingRow != null && typingRow.gameObject.activeSelf;
        if (hadTyping) SetTyping(false);

        var row = NewRow(mine ? "Me" : "Them", 10f);
        var bubble = NewImage(row, "Bubble", bubbleSprite, mine ? myBubble : theirBubble);
        bubble.type = Image.Type.Sliced; bubble.pixelsPerUnitMultiplier = 1.9f;
        var t = NewText(bubble.rectTransform, text, FontSize, mine ? Color.white : theirText, TextAlignmentOptions.TopLeft);
        t.enableWordWrapping = true;

        Vector2 pref = t.GetPreferredValues(text, BubbleMaxText, 0f);
        float w = Mathf.Min(pref.x, BubbleMaxText) + 2f;
        float h = t.GetPreferredValues(text, w, 0f).y;
        var br = bubble.rectTransform;
        br.anchorMin = br.anchorMax = new Vector2(mine ? 1f : 0f, 0.5f);
        br.pivot = new Vector2(mine ? 1f : 0f, 0.5f);
        br.anchoredPosition = new Vector2(mine ? -SideMargin : SideMargin, 0f);
        br.sizeDelta = new Vector2(w + PadX * 2f, h + PadY * 2f);
        var tr = t.rectTransform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(PadX, PadY); tr.offsetMax = new Vector2(-PadX, -PadY);
        row.sizeDelta = new Vector2(row.sizeDelta.x, br.sizeDelta.y);

        int sender = mine ? 1 : 0;
        AddRow(row, lastSender == sender ? 5f : 14f);
        lastSender = sender;

        if (playSound) Play(mine ? sendSound : receiveSound);
        if (IsOpen) StartCoroutine(PopIn(br));
        else br.localScale = Vector3.one;
    }

    // "..." Blase von ihr
    public void SetTyping(bool on)
    {
        Build();
        if (on)
        {
            if (typingRow == null)
            {
                typingRow = NewRow("Typing", 44f);
                var b = NewImage(typingRow, "Bubble", bubbleSprite, theirBubble);
                b.type = Image.Type.Sliced; b.pixelsPerUnitMultiplier = 1.9f;
                var br = b.rectTransform;
                br.anchorMin = br.anchorMax = br.pivot = new Vector2(0f, 0.5f);
                br.anchoredPosition = new Vector2(SideMargin, 0f);
                br.sizeDelta = new Vector2(74f, 44f);
                for (int i = 0; i < 3; i++)
                {
                    var d = NewImage(br, "Dot" + i, circleSprite, new Color(0.55f, 0.55f, 0.58f));
                    var dr = d.rectTransform;
                    dr.anchorMin = dr.anchorMax = new Vector2(0f, 0.5f);
                    dr.sizeDelta = new Vector2(10f, 10f);
                    dr.anchoredPosition = new Vector2(20f + i * 17f, 0f);
                }
            }
            typingRow.gameObject.SetActive(true);
            if (!rows.Contains(typingRow))
            {
                rows.Add(typingRow);
                rowSpacing.Add(lastSender == 0 ? 5f : 14f);
            }
        }
        else if (typingRow != null)
        {
            int idx = rows.IndexOf(typingRow);
            if (idx >= 0) { rows.RemoveAt(idx); rowSpacing.RemoveAt(idx); }
            typingRow.gameObject.SetActive(false);
        }
        Relayout();
    }

    // Text Buchstabe für Buchstabe ins Eingabefeld tippen
    public IEnumerator TypeInput(string text, float perChar = 0.045f)
    {
        Build();
        pendingInput = text;
        inputText.color = theirText;
        for (int i = 1; i <= text.Length; i++)
        {
            inputText.text = text.Substring(0, i) + "<color=#0A84FF>|</color>";
            yield return new WaitForSeconds(perChar * Random.Range(0.6f, 1.6f));
        }
        inputText.text = text;
    }

    // Eingabefeld als eigene Nachricht senden
    public void SendInput()
    {
        if (string.IsNullOrEmpty(pendingInput)) return;
        AddMessage(true, pendingInput, true);
        pendingInput = "";
        ResetInput();
    }

    public void SetHint(string text)
    {
        Build();
        hintText.text = string.IsNullOrEmpty(text) ? "" : InputBindings.FormatPrompt(text);
    }

    // ------------------------------------------------------------
    void Update()
    {
        if (typingRow != null && typingRow.gameObject.activeInHierarchy)
        {
            var bubble = typingRow.GetChild(0);
            for (int i = 0; i < 3 && i < bubble.childCount; i++)
            {
                float a = 0.35f + 0.65f * Mathf.Clamp01(Mathf.Sin(Time.unscaledTime * 6f - i * 0.9f) * 0.5f + 0.5f);
                bubble.GetChild(i).GetComponent<Image>().color = new Color(0.55f, 0.55f, 0.58f, a);
            }
        }
    }

    private IEnumerator FadeIn()
    {
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.25f;
            group.alpha = k;
            window.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, k);
            yield return null;
        }
        group.alpha = 1f; window.localScale = Vector3.one;
    }

    private IEnumerator FadeOut()
    {
        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / 0.2f; yield return null; }
        group.alpha = 0f;
        canvas.gameObject.SetActive(false);
    }

    private IEnumerator PopIn(RectTransform r)
    {
        for (float t = 0f; t < 0.15f; t += Time.unscaledDeltaTime)
        {
            if (r == null) yield break;
            r.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, t / 0.15f);
            yield return null;
        }
        if (r != null) r.localScale = Vector3.one;
    }

    private void Play(AudioClip c)
    {
        if (c == null) return;
        if (audioSrc == null) { audioSrc = gameObject.AddComponent<AudioSource>(); audioSrc.playOnAwake = false; audioSrc.spatialBlend = 0f; }
        audioSrc.PlayOneShot(c);
    }

    // ------------------------------------------------------------
    // Layout: Zeilen von unten nach oben stapeln, oben wird abgeschnitten
    private void AddRow(RectTransform row, float spacing)
    {
        if (typingRow != null && typingRow.gameObject.activeSelf)
        {
            // neue Zeile vor die "..."-Blase
            int ti = rows.IndexOf(typingRow);
            rows.Insert(ti, row); rowSpacing.Insert(ti, spacing);
        }
        else { rows.Add(row); rowSpacing.Add(spacing); }
        Relayout();
    }

    private void Relayout()
    {
        if (content == null) return;
        float total = 16f;
        for (int i = 0; i < rows.Count; i++) total += rows[i].sizeDelta.y + rowSpacing[i];
        content.sizeDelta = new Vector2(0f, total);
        float y = -8f;
        for (int i = 0; i < rows.Count; i++)
        {
            y -= rowSpacing[i];
            rows[i].anchoredPosition = new Vector2(0f, y);
            y -= rows[i].sizeDelta.y;
        }
    }

    private RectTransform NewRow(string name, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var r = (RectTransform)go.transform;
        r.SetParent(content, false);
        r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(0f, height);
        return r;
    }

    private static Image NewImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite; img.color = color; img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI NewText(Transform parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false;
        t.enableWordWrapping = false;
        return t;
    }

    private void ResetInput()
    {
        inputText.text = "Message";
        inputText.color = new Color(0.6f, 0.6f, 0.62f);
    }

    private void Build()
    {
        if (canvas != null) return;
        var go = new GameObject("PhoneChatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 1f;
        group = go.GetComponent<CanvasGroup>();
        group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false;
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        // ohne EventSystem kann man das Fenster nicht mit der Maus ziehen
        if (UnityEngine.EventSystems.EventSystem.current == null && FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

        // abgedunkelter Hintergrund
        var dim = NewImage(go.transform, "Dim", null, new Color(0.02f, 0.015f, 0.01f, 0.78f));
        var dr = dim.rectTransform; dr.anchorMin = Vector2.zero; dr.anchorMax = Vector2.one; dr.offsetMin = dr.offsetMax = Vector2.zero;

        // Fenster
        var win = NewImage(go.transform, "Window", windowSprite, windowColor);
        win.type = Image.Type.Sliced;
        window = win.rectTransform;
        window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = new Vector2(WinW, WinH);
        window.anchoredPosition = new Vector2(0f, 10f);
        var sh = win.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.45f); sh.effectDistance = new Vector2(6, -8);
        // Fenster mit der Maus verschiebbar
        win.raycastTarget = true;
        win.gameObject.AddComponent<ChatWindowDrag>().target = window;

        // Ampel-Punkte
        var dotsCol = new[] { new Color(1f, 0.37f, 0.34f), new Color(1f, 0.74f, 0.18f), new Color(0.16f, 0.78f, 0.25f) };
        for (int i = 0; i < 3; i++)
        {
            var d = NewImage(window, "Dot" + i, circleSprite, dotsCol[i]);
            var r = d.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(16f, 16f);
            r.anchoredPosition = new Vector2(24f + i * 24f, -20f);
        }

        // Titel
        var title = NewImage(window, "TitleBar", windowSprite, panelColor);
        title.type = Image.Type.Sliced; title.pixelsPerUnitMultiplier = 1.6f;
        var trr = title.rectTransform;
        trr.anchorMin = new Vector2(0f, 1f); trr.anchorMax = new Vector2(1f, 1f); trr.pivot = new Vector2(0.5f, 1f);
        trr.offsetMin = new Vector2(12f, -96f); trr.offsetMax = new Vector2(-12f, -40f);
        titleText = NewText(trr, "Messages", 25f, new Color(0.22f, 0.22f, 0.24f), TextAlignmentOptions.Center);
        var ttr = titleText.rectTransform; ttr.anchorMin = Vector2.zero; ttr.anchorMax = Vector2.one; ttr.offsetMin = ttr.offsetMax = Vector2.zero;

        // Chatbereich
        var chat = NewImage(window, "ChatPanel", windowSprite, panelColor);
        chat.type = Image.Type.Sliced; chat.pixelsPerUnitMultiplier = 1.6f;
        var cr = chat.rectTransform;
        cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one;
        cr.offsetMin = new Vector2(12f, 82f); cr.offsetMax = new Vector2(-12f, -106f);
        var vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vp.transform.SetParent(cr, false);
        viewport = (RectTransform)vp.transform;
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.offsetMin = new Vector2(0f, 4f); viewport.offsetMax = new Vector2(0f, -4f);
        var c = new GameObject("Content", typeof(RectTransform));
        c.transform.SetParent(viewport, false);
        content = (RectTransform)c.transform;
        content.anchorMin = new Vector2(0f, 0f); content.anchorMax = new Vector2(1f, 0f); content.pivot = new Vector2(0.5f, 0f);
        content.anchoredPosition = Vector2.zero;

        // Eingabefeld
        var input = NewImage(window, "Input", windowSprite, panelColor);
        input.type = Image.Type.Sliced; input.pixelsPerUnitMultiplier = 1.6f;
        var ir = input.rectTransform;
        ir.anchorMin = new Vector2(0f, 0f); ir.anchorMax = new Vector2(1f, 0f); ir.pivot = new Vector2(0.5f, 0f);
        ir.offsetMin = new Vector2(12f, 14f); ir.offsetMax = new Vector2(-12f, 68f);
        inputText = NewText(ir, "Message", 21f, Color.gray, TextAlignmentOptions.MidlineLeft);
        var itr = inputText.rectTransform; itr.anchorMin = Vector2.zero; itr.anchorMax = Vector2.one; itr.offsetMin = new Vector2(16f, 0f); itr.offsetMax = new Vector2(-16f, 0f);
        ResetInput();

        // Hinweis unter dem Fenster
        hintText = NewText(go.transform, "", 22f, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.Center);
        var hr = hintText.rectTransform;
        hr.anchorMin = new Vector2(0f, 0f); hr.anchorMax = new Vector2(1f, 0f); hr.pivot = new Vector2(0.5f, 0f);
        hr.anchoredPosition = new Vector2(0f, 24f); hr.sizeDelta = new Vector2(0f, 36f);

        go.SetActive(false);
    }
}
