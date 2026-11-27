using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================================
// HANDY-DISPLAY - Story 2 spielt im November 1998
// Statt Chat-App ein altes Tasten-Handy mit gruenem LCD:
//  - keine Sprechblasen, kein "schreibt gerade ..." (gab es noch nicht)
//  - Tippen per Mehrfach-Tastendruck (T9 gab es noch nicht) mit Tastentoenen
//  - "Message sent" / "1 message received" wie bei SMS damals
// Wird komplett per Code gebaut. Benutzung (wie vorher):
//   chat.Show("Emilia");
//   chat.AddTimestamp("Today 08:52 PM");
//   chat.AddMessage(false, "Are you already on the way?");   // false = sie, true = ich
//   yield return chat.TypeInput("leaving right now");        // tippt per Tasten ins Display
//   chat.SendInput();                                         // Eingabe senden
//   chat.SetTyping(true);                                     // 1998: keine Anzeige
//   chat.SetHint("[E] Senden");
//   chat.Hide();
// =====================================================
public class PhoneChatUI : MonoBehaviour
{
    [Header("Sprites (abgerundete Ecken, optional)")]
    [Tooltip("Fuer die Tasten")]
    public Sprite bubbleSprite;
    [Tooltip("Fuer Gehaeuse und Display")]
    public Sprite windowSprite;
    [Tooltip("Wird nicht mehr benutzt")]
    public Sprite circleSprite;

    [Header("Display-Schrift (optional, z. B. Pixel-Font)")]
    public TMP_FontAsset lcdFont;

    [Header("Sounds (optional)")]
    public AudioClip receiveSound;
    public AudioClip sendSound;
    [Tooltip("Alter Tipp-Loop - wird nur benutzt, wenn 'Keypad Beeps' aus ist")]
    public AudioClip typingSound;
    [Range(0f, 1f)] public float typingVolume = 0.8f;
    [Tooltip("Kurzer Piep pro Tastendruck, wie bei Handys von 1998")]
    public bool keypadBeeps = true;
    [Range(0f, 1f)] public float beepVolume = 0.3f;

    [Header("Farben (Handy 1998)")]
    public Color bodyColor = new Color(0.12f, 0.14f, 0.18f);
    public Color bezelColor = new Color(0.04f, 0.045f, 0.05f);
    public Color lcdColor = new Color(0.60f, 0.69f, 0.45f);
    public Color lcdInk = new Color(0.10f, 0.14f, 0.08f);
    public Color keyColor = new Color(0.27f, 0.30f, 0.34f);
    public Color keyTextColor = new Color(0.86f, 0.88f, 0.86f);

    public bool IsOpen { get; private set; }

    // Masse (Referenz 1920x1080). Das Gehaeuse ragt unten aus dem Bild,
    // als haette Jacob das Handy in der Hand.
    const float BodyW = 640f, BodyH = 1300f, BodyY = -210f;
    const float LcdW = 520f, LcdH = 450f, LcdY = 140f, LcdPad = 14f;
    const float HeaderH = 40f, InputH = 116f;
    const float MsgSize = 26f, SmallSize = 18f;
    const int MaxChars = 160;            // SMS-Limit
    const float SlideOffset = 260f;

    // Tastenbelegung fuer Mehrfach-Tastendruck (Index = Taste)
    static readonly string[] KeyLetters = { " 0", ".,'?!-1", "abc2", "def3", "ghi4", "jkl5", "mno6", "pqrs7", "tuv8", "wxyz9" };

    private class Entry { public RectTransform row; public TextMeshProUGUI text; public bool mine; public string body; }

    private Canvas canvas;
    private CanvasGroup group;
    private RectTransform phone, content, banner;
    private TextMeshProUGUI headerText, inputText, modeText, counterText, softkeyText, bannerText, hintText;
    private readonly List<RectTransform> rows = new List<RectTransform>();
    private readonly List<float> rowSpacing = new List<float>();
    private readonly List<Entry> entries = new List<Entry>();
    private string contact = "";
    private AudioSource audioSrc, typingSrc;
    private string pendingInput = "", typed = "";
    private bool composing, keying, cursorOn = true;
    private int bannerToken;
    private static AudioClip keyBeep, msgBeep;

    // ------------------------------------------------------------
    public void Show(string contactName)
    {
        Build();
        contact = contactName ?? "";
        headerText.text = "<noparse>" + contact + "</noparse>";
        RefreshEntries();
        if (!composing) softkeyText.text = "Reply";
        canvas.gameObject.SetActive(true);
        IsOpen = true;
        StopAllCoroutines();
        banner.gameObject.SetActive(false);
        StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        if (!IsOpen) return;
        IsOpen = false;
        StopAllCoroutines();
        StopTypingSound();
        StartCoroutine(FadeOut());
    }

    public void Clear()
    {
        Build();
        foreach (var r in rows)
        {
            if (r == null) continue;
            if (Application.isPlaying) Destroy(r.gameObject); else DestroyImmediate(r.gameObject);
        }
        rows.Clear(); rowSpacing.Clear(); entries.Clear();
        ResetInput();
        softkeyText.text = "Reply";
        banner.gameObject.SetActive(false);
        Relayout();
    }

    public void AddTimestamp(string text)
    {
        Build();
        var row = NewRow("Time", SmallSize + 8f);
        var t = NewText(row, "<noparse>" + text + "</noparse>", SmallSize, Faded(lcdInk, 0.7f), TextAlignmentOptions.Center, lcdFont);
        Stretch(t.rectTransform);
        AddRow(row, rows.Count == 0 ? 0f : 10f);
    }

    // mine = true -> von Jacob gesendet
    public void AddMessage(bool mine, string text, bool playSound = false)
    {
        Build();
        var row = NewRow(mine ? "Me" : "Them", MsgSize);
        var t = NewText(row, "", MsgSize, lcdInk, TextAlignmentOptions.TopLeft, lcdFont);
        t.enableWordWrapping = true;
        Stretch(t.rectTransform);
        var e = new Entry { row = row, text = t, mine = mine, body = text ?? "" };
        entries.Add(e);
        ApplyEntry(e);
        AddRow(row, rows.Count == 0 ? 0f : 8f);

        if (!playSound) return;
        if (mine)
        {
            Play(sendSound);
            ShowBanner("Message sent");
        }
        else
        {
            Play(receiveSound != null ? receiveSound : MsgBeep());
            ShowBanner("1 message\nreceived");
        }
    }

    // 1998 gab es kein "schreibt gerade ..." - man wartet einfach auf die SMS.
    public void SetTyping(bool on) { }

    // Text per Mehrfach-Tastendruck eintippen (z. B. 3x Taste 5 fuer "l")
    public IEnumerator TypeInput(string text, float perChar = 0.045f)
    {
        Build();
        pendingInput = text ?? "";
        typed = "";
        composing = true;
        keying = true;
        modeText.gameObject.SetActive(true);
        counterText.gameObject.SetActive(true);
        inputText.gameObject.SetActive(true);
        softkeyText.text = "";
        UpdateInput(null);
        if (!keypadBeeps) StartTypingSound();

        float press = perChar * 0.8f;
        for (int i = 0; i < pendingInput.Length; i++)
        {
            char c = pendingInput[i];
            int key = KeyFor(c, out int presses);
            string letters = key >= 0 ? KeyLetters[key] : c.ToString();
            for (int p = 0; p < presses; p++)
            {
                char cand = letters[p];
                if (char.IsUpper(c)) cand = char.ToUpperInvariant(cand);
                UpdateInput(cand.ToString());
                if (keypadBeeps) Play(KeyBeep(), beepVolume);
                yield return new WaitForSeconds(press * Random.Range(0.75f, 1.25f));
            }
            typed += c;
            UpdateInput(null);
            // gleiche Taste direkt danach -> kurz warten, bis der Cursor weiterspringt
            bool sameKeyNext = key >= 0 && i + 1 < pendingInput.Length && KeyFor(pendingInput[i + 1], out _) == key;
            yield return new WaitForSeconds(perChar * (sameKeyNext ? 2f : 0.5f) * Random.Range(0.7f, 1.3f));
        }
        StopTypingSound();
        keying = false;
        UpdateInput(null);
        softkeyText.text = "Send";
    }

    // Eingabe als eigene SMS senden
    public void SendInput()
    {
        if (string.IsNullOrEmpty(pendingInput)) return;
        string text = pendingInput;
        ResetInput();
        AddMessage(true, text, true);
        softkeyText.text = "Back";
    }

    public void SetHint(string text)
    {
        Build();
        hintText.text = string.IsNullOrEmpty(text) ? "" : InputBindings.FormatPrompt(text);
    }

    // ------------------------------------------------------------
    void Update()
    {
        // blinkender Cursor, wenn fertig getippt (aber noch nicht gesendet)
        if (!composing || keying || inputText == null) return;
        bool on = Mathf.Repeat(Time.unscaledTime, 1f) < 0.55f;
        if (on == cursorOn) return;
        cursorOn = on;
        UpdateInput(null);
    }

    void OnDisable() { StopTypingSound(); }

    private void UpdateInput(string candidate)
    {
        if (inputText == null) return;
        string s = "<noparse>" + typed + "</noparse>";
        if (candidate != null) s += "<u><noparse>" + candidate + "</noparse></u>";
        else if (keying || cursorOn) s += "_";
        inputText.text = s;
        counterText.text = (MaxChars - typed.Length - (candidate != null ? 1 : 0)).ToString();
    }

    private void ResetInput()
    {
        composing = false;
        keying = false;
        pendingInput = "";
        typed = "";
        if (inputText == null) return;
        inputText.text = "";
        inputText.gameObject.SetActive(false);
        modeText.gameObject.SetActive(false);
        counterText.gameObject.SetActive(false);
    }

    private static int KeyFor(char c, out int presses)
    {
        char lc = char.ToLowerInvariant(c);
        for (int k = 0; k < KeyLetters.Length; k++)
        {
            int idx = KeyLetters[k].IndexOf(lc);
            if (idx >= 0) { presses = idx + 1; return k; }
        }
        presses = 1;
        return -1;
    }

    private void ApplyEntry(Entry e)
    {
        string who = e.mine ? "ME" : (string.IsNullOrEmpty(contact) ? "?" : contact.ToUpperInvariant());
        string full = "<b><noparse>" + who + ":</noparse></b> <noparse>" + e.body + "</noparse>";
        e.text.text = full;
        float h = e.text.GetPreferredValues(full, LcdW - LcdPad * 2f, 0f).y;
        e.row.sizeDelta = new Vector2(0f, Mathf.Ceil(h) + 1f);
    }

    private void RefreshEntries()
    {
        foreach (var e in entries) if (e.row != null) ApplyEntry(e);
        Relayout();
    }

    // ------------------------------------------------------------
    private void ShowBanner(string text)
    {
        if (!IsOpen || banner == null) return;
        StartCoroutine(BannerRoutine(text));
    }

    private IEnumerator BannerRoutine(string text)
    {
        int my = ++bannerToken;
        bannerText.text = text;
        banner.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.1f);
        if (my == bannerToken && banner != null) banner.gameObject.SetActive(false);
    }

    private IEnumerator FadeIn()
    {
        const float dur = 0.3f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = t / dur;
            float e = 1f - (1f - k) * (1f - k) * (1f - k);
            group.alpha = k;
            phone.anchoredPosition = new Vector2(0f, Mathf.Lerp(-SlideOffset, 0f, e));
            yield return null;
        }
        group.alpha = 1f;
        phone.anchoredPosition = Vector2.zero;
    }

    private IEnumerator FadeOut()
    {
        const float dur = 0.22f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = t / dur;
            group.alpha = 1f - k;
            phone.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -SlideOffset, k * k));
            yield return null;
        }
        group.alpha = 0f;
        canvas.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------
    // Sounds
    private void Play(AudioClip c, float volume = 1f)
    {
        if (c == null) return;
        if (audioSrc == null) { audioSrc = gameObject.AddComponent<AudioSource>(); audioSrc.playOnAwake = false; audioSrc.spatialBlend = 0f; }
        audioSrc.PlayOneShot(c, volume);
    }

    private void StartTypingSound()
    {
        if (typingSound == null) return;
        if (typingSrc == null) { typingSrc = gameObject.AddComponent<AudioSource>(); typingSrc.playOnAwake = false; typingSrc.spatialBlend = 0f; }
        typingSrc.clip = typingSound;
        typingSrc.loop = true;
        typingSrc.volume = typingVolume;
        typingSrc.time = Random.Range(0f, typingSound.length * 0.5f);
        typingSrc.Play();
    }

    private void StopTypingSound()
    {
        if (typingSrc != null) typingSrc.Stop();
    }

    private static AudioClip KeyBeep()
    {
        if (keyBeep == null) keyBeep = MakeTone("KeypadBeep", 1250f, 0.045f, 1, 0f);
        return keyBeep;
    }

    private static AudioClip MsgBeep()
    {
        if (msgBeep == null) msgBeep = MakeTone("MessageBeep", 1650f, 0.09f, 2, 0.07f);
        return msgBeep;
    }

    // einfacher Piezo-Piepton (kein Sound-Asset noetig)
    private static AudioClip MakeTone(string name, float freq, float duration, int pulses, float gap)
    {
        const int rate = 22050;
        int len = Mathf.CeilToInt(duration * rate), gapLen = Mathf.CeilToInt(gap * rate);
        int total = pulses * len + (pulses - 1) * gapLen;
        var data = new float[total];
        for (int p = 0; p < pulses; p++)
        {
            int off = p * (len + gapLen);
            for (int i = 0; i < len; i++)
            {
                float env = Mathf.Clamp01(i / (rate * 0.003f)) * Mathf.Clamp01((len - i) / (rate * 0.008f));
                float s = Mathf.Sin(2f * Mathf.PI * freq * i / rate);
                data[off + i] = (0.6f * s + 0.4f * Mathf.Sign(s)) * 0.45f * env;
            }
        }
        var clip = AudioClip.Create(name, total, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ------------------------------------------------------------
    // Layout: Zeilen von unten nach oben stapeln, oben wird abgeschnitten
    private void AddRow(RectTransform row, float spacing)
    {
        rows.Add(row);
        rowSpacing.Add(spacing);
        Relayout();
    }

    private void Relayout()
    {
        if (content == null) return;
        float total = 8f;
        for (int i = 0; i < rows.Count; i++) total += rows[i].sizeDelta.y + rowSpacing[i];
        content.sizeDelta = new Vector2(0f, total);
        float y = -4f;
        for (int i = 0; i < rows.Count; i++)
        {
            y -= rowSpacing[i];
            rows[i].anchoredPosition = new Vector2(0f, y);
            y -= rows[i].sizeDelta.y;
        }
    }

    private RectTransform NewRow(string name, float height)
    {
        var r = NewRect(content, name);
        r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f); r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(0f, height);
        return r;
    }

    private static RectTransform NewRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(Transform parent, string name, Sprite sprite, Color color, float ppuMultiplier = 0f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (sprite != null && ppuMultiplier > 0f)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppuMultiplier;
        }
        return img;
    }

    private static TextMeshProUGUI NewText(Transform parent, string text, float size, Color color, TextAlignmentOptions align, TMP_FontAsset font = null)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false;
        t.enableWordWrapping = false;
        return t;
    }

    private static RectTransform Place(Graphic g, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var r = g.rectTransform;
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = pivot;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    private static void Stretch(RectTransform r, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top);
    }

    private static Color Faded(Color c, float a) => new Color(c.r, c.g, c.b, a);
    private static Color Lighter(Color c, float d) => new Color(Mathf.Min(1f, c.r + d), Mathf.Min(1f, c.g + d), Mathf.Min(1f, c.b + d), c.a);

    // waagerechte Trennlinie im LCD
    private void Line(RectTransform parent, bool fromTop, float inset)
    {
        var r = NewImage(parent, "Line", null, lcdInk).rectTransform;
        float y = fromTop ? 1f : 0f;
        r.anchorMin = new Vector2(0f, y); r.anchorMax = new Vector2(1f, y); r.pivot = new Vector2(0.5f, y);
        r.sizeDelta = new Vector2(-LcdPad * 2f, 2f);
        r.anchoredPosition = new Vector2(0f, fromTop ? -inset : inset);
    }

    private void Key(RectTransform parent, Vector2 pos, Vector2 size, string label, Color color)
    {
        var c = new Vector2(0.5f, 0.5f);
        var k = Place(NewImage(parent, "Key", bubbleSprite, color, 2f), c, c, pos, size);
        if (string.IsNullOrEmpty(label)) return;
        var t = NewText(k, label, 30f, keyTextColor, TextAlignmentOptions.Center);
        Stretch(t.rectTransform);
    }

    // kleiner Briefumschlag fuer das SMS-Popup
    private void Envelope(RectTransform parent, Vector2 pos)
    {
        var c = new Vector2(0.5f, 0.5f);
        var e = NewRect(parent, "Envelope");
        e.anchorMin = e.anchorMax = e.pivot = c;
        e.anchoredPosition = pos;
        e.sizeDelta = new Vector2(48f, 32f);
        Place(NewImage(e, "Top", null, lcdColor), c, c, new Vector2(0f, 14.5f), new Vector2(48f, 3f));
        Place(NewImage(e, "Bottom", null, lcdColor), c, c, new Vector2(0f, -14.5f), new Vector2(48f, 3f));
        Place(NewImage(e, "Left", null, lcdColor), c, c, new Vector2(-22.5f, 0f), new Vector2(3f, 32f));
        Place(NewImage(e, "Right", null, lcdColor), c, c, new Vector2(22.5f, 0f), new Vector2(3f, 32f));
        Place(NewImage(e, "FlapL", null, lcdColor), c, c, new Vector2(-12f, 6f), new Vector2(30f, 3f)).localRotation = Quaternion.Euler(0f, 0f, -37f);
        Place(NewImage(e, "FlapR", null, lcdColor), c, c, new Vector2(12f, 6f), new Vector2(30f, 3f)).localRotation = Quaternion.Euler(0f, 0f, 37f);
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

        var C = new Vector2(0.5f, 0.5f);

        // abgedunkelter Hintergrund
        Stretch(NewImage(go.transform, "Dim", null, new Color(0.02f, 0.015f, 0.01f, 0.78f)).rectTransform);

        // Handy - alles darin wird beim Oeffnen von unten hochgeschoben
        phone = NewRect(go.transform, "Phone");
        phone.anchorMin = phone.anchorMax = phone.pivot = C;
        phone.sizeDelta = Vector2.zero;

        // Gehaeuse
        var body = NewImage(phone, "Body", windowSprite, bodyColor, 0.5f);
        Place(body, C, C, new Vector2(0f, BodyY), new Vector2(BodyW, BodyH));
        var sh = body.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.5f);
        sh.effectDistance = new Vector2(8f, -10f);

        // Hoermuschel
        Place(NewImage(phone, "Earpiece", bubbleSprite, bezelColor, 4f), C, C, new Vector2(0f, BodyY + BodyH * 0.5f - 24f), new Vector2(110f, 12f));

        // Display-Rahmen und LCD
        Place(NewImage(phone, "Bezel", windowSprite, bezelColor, 1.2f), C, C, new Vector2(0f, LcdY), new Vector2(LcdW + 44f, LcdH + 52f));
        var lcd = Place(NewImage(phone, "LCD", windowSprite, lcdColor, 4f), C, C, new Vector2(0f, LcdY), new Vector2(LcdW, LcdH));

        // Kopfzeile: Netz, Name, Akku
        var head = NewRect(lcd, "Header");
        head.anchorMin = new Vector2(0f, 1f); head.anchorMax = new Vector2(1f, 1f); head.pivot = new Vector2(0.5f, 1f);
        head.offsetMin = new Vector2(LcdPad, -(LcdPad + HeaderH)); head.offsetMax = new Vector2(-LcdPad, -LcdPad);
        for (int i = 0; i < 4; i++)
            Place(NewImage(head, "Signal" + i, null, lcdInk), Vector2.zero, Vector2.zero, new Vector2(i * 9f, 6f), new Vector2(6f, 8f + i * 5f));
        var BR = new Vector2(1f, 0f);
        Place(NewImage(head, "BatteryTip", null, lcdInk), BR, BR, new Vector2(0f, 11f), new Vector2(4f, 8f));
        Place(NewImage(head, "Battery", null, lcdInk), BR, BR, new Vector2(-4f, 6f), new Vector2(34f, 18f));
        Place(NewImage(head, "BatteryInner", null, lcdColor), BR, BR, new Vector2(-6f, 8f), new Vector2(30f, 14f));
        for (int i = 0; i < 3; i++)
            Place(NewImage(head, "BatteryBar" + i, null, lcdInk), BR, BR, new Vector2(-8f - i * 9f, 10f), new Vector2(7f, 10f));
        headerText = NewText(head, "", MsgSize, lcdInk, TextAlignmentOptions.Center, lcdFont);
        Stretch(headerText.rectTransform, 50f, 0f, 50f, 0f);
        Line(lcd, true, LcdPad + HeaderH + 3f);

        // Nachrichten (neueste unten, alte verschwinden oben)
        var vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vp.transform.SetParent(lcd, false);
        Stretch((RectTransform)vp.transform, LcdPad, LcdPad + InputH + 8f, LcdPad, LcdPad + HeaderH + 8f);
        content = NewRect(vp.transform, "Content");
        content.anchorMin = new Vector2(0f, 0f); content.anchorMax = new Vector2(1f, 0f); content.pivot = new Vector2(0.5f, 0f);
        content.anchoredPosition = Vector2.zero;

        // Eingabe: "abc" + Zeichenzaehler, Text, Softkey
        Line(lcd, false, LcdPad + InputH + 3f);
        var inp = NewRect(lcd, "Input");
        inp.anchorMin = new Vector2(0f, 0f); inp.anchorMax = new Vector2(1f, 0f); inp.pivot = new Vector2(0.5f, 0f);
        inp.offsetMin = new Vector2(LcdPad, LcdPad); inp.offsetMax = new Vector2(-LcdPad, LcdPad + InputH);
        var TL = new Vector2(0f, 1f); var TR = new Vector2(1f, 1f); var B = new Vector2(0.5f, 0f);
        modeText = NewText(inp, "abc", SmallSize, lcdInk, TextAlignmentOptions.TopLeft, lcdFont);
        Place(modeText, TL, TL, Vector2.zero, new Vector2(120f, SmallSize + 4f));
        counterText = NewText(inp, "160", SmallSize, lcdInk, TextAlignmentOptions.TopRight, lcdFont);
        Place(counterText, TR, TR, Vector2.zero, new Vector2(120f, SmallSize + 4f));
        inputText = NewText(inp, "", MsgSize, lcdInk, TextAlignmentOptions.TopLeft, lcdFont);
        inputText.enableWordWrapping = true;
        Stretch(inputText.rectTransform, 0f, MsgSize + 4f, 0f, SmallSize + 6f);
        softkeyText = NewText(inp, "Reply", MsgSize, lcdInk, TextAlignmentOptions.Bottom, lcdFont);
        Place(softkeyText, B, B, Vector2.zero, new Vector2(LcdW - LcdPad * 2f, MsgSize + 4f));

        // Popup "Message sent" / "1 message received" (invertiert)
        float msgCenter = ((LcdPad + InputH + 8f) - (LcdPad + HeaderH + 8f)) * 0.5f;
        banner = Place(NewImage(lcd, "Popup", windowSprite, lcdInk, 4f), C, C, new Vector2(0f, msgCenter), new Vector2(LcdW - 60f, 112f));
        Envelope(banner, new Vector2(-(LcdW - 60f) * 0.5f + 52f, 0f));
        bannerText = NewText(banner, "", MsgSize, lcdColor, TextAlignmentOptions.Center, lcdFont);
        Stretch(bannerText.rectTransform, 96f, 0f, 16f, 0f);
        banner.gameObject.SetActive(false);

        // Tasten unter dem Display (unten vom Bildrand abgeschnitten)
        float naviY = LcdY - (LcdH + 52f) * 0.5f - 64f;
        Key(phone, new Vector2(0f, naviY), new Vector2(210f, 58f), "", Lighter(keyColor, 0.08f));
        Key(phone, new Vector2(-205f, naviY), new Vector2(90f, 50f), "", keyColor);
        Key(phone, new Vector2(205f, naviY), new Vector2(90f, 50f), "C", keyColor);
        string[] labels = { "1", "2<size=55%> abc</size>", "3<size=55%> def</size>", "4<size=55%> ghi</size>", "5<size=55%> jkl</size>", "6<size=55%> mno</size>", "7<size=55%> pqrs</size>", "8<size=55%> tuv</size>", "9<size=55%> wxyz</size>", "*", "0<size=55%> +</size>", "#" };
        for (int i = 0; i < labels.Length; i++)
            Key(phone, new Vector2((i % 3 - 1) * 180f, naviY - 88f - (i / 3) * 84f), new Vector2(156f, 62f), labels[i], keyColor);

        // Hinweis rechts neben dem Handy
        hintText = NewText(go.transform, "", 26f, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.Left);
        Place(hintText, C, new Vector2(0f, 0.5f), new Vector2(BodyW * 0.5f + 48f, LcdY), new Vector2(560f, 60f));

        ResetInput();
        go.SetActive(false);
    }
}
