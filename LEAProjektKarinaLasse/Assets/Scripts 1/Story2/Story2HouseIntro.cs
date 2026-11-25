using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// =====================================================
// STORY 2 - ANFANG: JACOBS HAUS
//  1. Einführungstext auf Schwarz
//  2. Jacob ist auf der Couch eingeschlafen, das Handy vibriert
//  3. [E] Handy nehmen -> Chat mit Emilia ("Are you already on the way?")
//     -> antworten -> sie antwortet -> Handy weglegen
//  4. Autoschlüssel + Jacke holen
//  5. Raus zum Auto -> [E] Einsteigen -> Tankstellen-Szene (Fahrt)
// =====================================================
public class Story2HouseIntro : MonoBehaviour
{
    [System.Serializable]
    public class ChatLine
    {
        public enum Who { Timestamp, Emilia, Jacob }
        public Who who;
        [TextArea(1, 3)] public string text;
        public ChatLine(Who w, string t) { who = w; text = t; }
    }

    [Header("Objekte")]
    public GameObject player;
    public CarController car;
    public HouseStoryItem phone;
    public HouseStoryItem keys;
    public HouseStoryItem jacket;
    public HouseStoryItem carDoor;
    [Tooltip("Leuchtender Bildschirm des Handys (an, solange es vibriert)")]
    public Renderer phoneScreen;
    public PhoneChatUI chat;

    [Header("UI")]
    public TextMeshProUGUI introText;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI taskText;
    public TextMeshProUGUI promptText;
    public CanvasGroup blackScreen;

    [Header("Sound")]
    public AudioSource playerVoiceAudioSource;
    public AudioClip playerTalking;
    public AudioSource phoneAudio;          // 3D am Handy
    public AudioClip vibrateSound;
    public AudioClip pickupSound;           // optional (Schlüssel/Jacke)
    public AudioClip carDoorSound;          // optional
    public AudioClip engineStartSound;      // optional

    [Header("Einführung")]
    public bool playIntroStory = true;
    [TextArea(3, 8)]
    public string introStory =
        "Nobody believed me when I told them what happened that night. It was November 1998, just a few weeks after that girl went missing in Emschurches. " +
        "I was on my way to see Emilia, driving the old country road... and I never should have stopped.";
    public float introTypeSpeed = 0.035f;
    public float introHoldAfterTyping = 3.5f;

    [Header("Chat mit Emilia")]
    public string contactName = "Emilia";
    public List<ChatLine> chatHistory = new List<ChatLine>
    {
        new ChatLine(ChatLine.Who.Timestamp, "Yesterday 10:14 PM"),
        new ChatLine(ChatLine.Who.Emilia, "are you still up?"),
        new ChatLine(ChatLine.Who.Jacob, "yeah can't sleep"),
        new ChatLine(ChatLine.Who.Emilia, "me neither. i miss you"),
        new ChatLine(ChatLine.Who.Jacob, "miss you too"),
        new ChatLine(ChatLine.Who.Emilia, "come over tomorrow? my parents are gone for the weekend"),
        new ChatLine(ChatLine.Who.Jacob, "deal. i'll drive over after work"),
        new ChatLine(ChatLine.Who.Timestamp, "Today 06:41 PM"),
        new ChatLine(ChatLine.Who.Jacob, "just got home, gonna lie down for a sec"),
        new ChatLine(ChatLine.Who.Emilia, "ok! drive safe, the roads out here are pitch black at night"),
        new ChatLine(ChatLine.Who.Emilia, "and fill up your tank this time lol"),
        new ChatLine(ChatLine.Who.Timestamp, "Today 08:52 PM"),
        new ChatLine(ChatLine.Who.Emilia, "Are you already on the way?"),
    };
    public string reply = "leaving right now, sorry! fell asleep on the couch";
    public string[] emiliaAnswers = { "of course you did", "text me when you're close <3" };

    [Header("Texte (Jacob)")]
    public string[] linesWakeUp = { "...huh? Did I fall asleep?" };
    public string[] linesAfterPhone = { "Crap, I'm late.", "Keys, jacket, and I'm out." };
    public string[] linesKeys = { "Got the keys." };
    public string[] linesJacket = { "It's freezing out there tonight." };
    public string[] linesReady = { "Alright. Let's go." };

    [Header("Aufgaben")]
    public string taskPhone = "Aufs Handy schauen";
    public string taskItems = "Autoschlüssel und Jacke holen";
    public string taskCar = "Zum Auto gehen";

    [Header("Danach")]
    public string nextScene = "GasStationScene";
    public float textSpeed = 0.045f;
    public float dialogueEndDelay = 1.5f;

    private enum Phase { Intro, Phone, Items, Car, Done }
    private Phase phase = Phase.Intro;
    private int itemsTaken;
    private bool talking;
    private readonly List<Behaviour> frozen = new List<Behaviour>();

    // ------------------------------------------------------------
    void Start()
    {
        Hide(dialogueText); Hide(taskText); Hide(promptText); Hide(introText);
        if (blackScreen != null) { blackScreen.gameObject.SetActive(true); blackScreen.alpha = 1f; }
        if (car != null)
        {
            car.controlsEnabled = false;
            car.engineRunning = false;
            if (car.driverCamera != null) car.driverCamera.gameObject.SetActive(false);
        }
        SetPhoneGlow(false);
        foreach (var it in new[] { phone, keys, jacket, carDoor }) if (it != null) it.story = this;
        StartCoroutine(Sequence());
    }

    public bool CanUse(HouseStoryItem item)
    {
        switch (item.kind)
        {
            case HouseStoryItem.Kind.Phone: return phase == Phase.Phone;
            case HouseStoryItem.Kind.Keys:
            case HouseStoryItem.Kind.Jacket: return phase == Phase.Items;
            case HouseStoryItem.Kind.Car: return phase == Phase.Car;
        }
        return false;
    }

    public void OnItemUsed(HouseStoryItem item)
    {
        switch (item.kind)
        {
            case HouseStoryItem.Kind.Phone:
                item.Used = true;
                StartCoroutine(PhoneRoutine());
                break;
            case HouseStoryItem.Kind.Keys:
            case HouseStoryItem.Kind.Jacket:
                item.Used = true;
                if (item.hideOnPickup) item.gameObject.SetActive(false);
                if (pickupSound != null && phoneAudio != null) phoneAudio.PlayOneShot(pickupSound);
                itemsTaken++;
                StartCoroutine(ItemRoutine(item));
                break;
            case HouseStoryItem.Kind.Car:
                item.Used = true;
                StartCoroutine(EnterCar());
                break;
        }
    }

    // ------------------------------------------------------------
    private IEnumerator Sequence()
    {
        Freeze(true);
        if (playIntroStory && introText != null && !string.IsNullOrEmpty(introStory))
        {
            yield return new WaitForSeconds(1f);
            yield return TypeIntro(introStory);
            yield return new WaitForSeconds(introHoldAfterTyping);
            Hide(introText);
            yield return new WaitForSeconds(0.5f);
        }

        // Handy vibriert schon beim Aufwachen
        phase = Phase.Phone;
        StartCoroutine(Vibrate());
        yield return Fade(1f, 0f, 2.5f);
        Freeze(false);
        yield return Say(linesWakeUp);
        // nur anzeigen, wenn das Handy nicht schon in der Hand ist
        if (phase == Phase.Phone && phone != null && !phone.Used && !chat.IsOpen) ShowTask(taskPhone);
    }

    private IEnumerator Vibrate()
    {
        while (phase == Phase.Phone && phone != null && !phone.Used)
        {
            SetPhoneGlow(true);
            if (phoneAudio != null && vibrateSound != null) phoneAudio.PlayOneShot(vibrateSound);
            // Handy zittert auf dem Tisch
            Vector3 p0 = phone.transform.localPosition; Quaternion r0 = phone.transform.localRotation;
            for (float t = 0f; t < 1.4f && !phone.Used; t += Time.deltaTime)
            {
                bool on = t < 0.5f || (t > 0.75f && t < 1.25f);
                phone.transform.localPosition = p0 + (on ? new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * 0.0015f : Vector3.zero);
                phone.transform.localRotation = r0 * Quaternion.Euler(0f, on ? Random.Range(-1.2f, 1.2f) : 0f, 0f);
                yield return null;
            }
            phone.transform.localPosition = p0; phone.transform.localRotation = r0;
            for (float t = 0f; t < 3.5f && !phone.Used; t += Time.deltaTime) yield return null;
        }
    }

    private IEnumerator PhoneRoutine()
    {
        Freeze(true);
        Hide(dialogueText);
        HideTask();
        SetPhoneGlow(false);
        phone.gameObject.SetActive(false);   // Jacob hat es in der Hand

        chat.Clear();
        foreach (var l in chatHistory)
        {
            if (l.who == ChatLine.Who.Timestamp) chat.AddTimestamp(l.text);
            else chat.AddMessage(l.who == ChatLine.Who.Jacob, l.text);
        }
        chat.SetHint("");
        chat.Show(contactName);
        yield return new WaitForSeconds(1.2f);

        // antworten
        chat.SetHint("[E] Antworten");
        yield return WaitKey();
        chat.SetHint("");
        yield return chat.TypeInput(reply);
        chat.SetHint("[E] Senden");
        yield return WaitKey();
        chat.SetHint("");
        chat.SendInput();

        // Emilia antwortet
        yield return new WaitForSeconds(1.0f);
        foreach (var a in emiliaAnswers)
        {
            chat.SetTyping(true);
            yield return new WaitForSeconds(1.2f + a.Length * 0.03f);
            chat.AddMessage(false, a, true);
            yield return new WaitForSeconds(0.6f);
        }
        yield return new WaitForSeconds(0.6f);
        chat.SetHint("[E] Handy weglegen");
        yield return WaitKey();
        chat.SetHint("");
        chat.Hide();
        yield return new WaitForSeconds(0.25f);
        Freeze(false);

        phase = Phase.Items;
        ShowItemsTask();
        yield return Say(linesAfterPhone);
    }

    private IEnumerator ItemRoutine(HouseStoryItem item)
    {
        if (itemsTaken < 2) ShowItemsTask();
        yield return Say(item.kind == HouseStoryItem.Kind.Keys ? linesKeys : linesJacket);
        if (itemsTaken >= 2 && phase == Phase.Items)
        {
            phase = Phase.Car;
            ShowTask(taskCar);
            yield return Say(linesReady);
        }
    }

    private void ShowItemsTask() => ShowTask(taskItems + " (" + itemsTaken + "/2)");

    private IEnumerator EnterCar()
    {
        phase = Phase.Done;
        Freeze(true);
        HideTask();
        if (phoneAudio != null && carDoorSound != null) phoneAudio.PlayOneShot(carDoorSound);
        yield return Fade(0f, 1f, 1.2f);
        if (engineStartSound != null)
        {
            AudioSource.PlayClipAtPoint(engineStartSound, car != null ? car.transform.position : Vector3.zero);
            yield return new WaitForSeconds(1.5f);
        }
        yield return new WaitForSeconds(0.5f);
        SceneLoader.Load(nextScene);
    }

    // ------------------------------------------------------------
    // Wie E drücken (z. B. für Tests oder andere Scripts)
    public void Continue() => continueRequested = true;
    private bool continueRequested;

    private IEnumerator WaitKey()
    {
        yield return null;
        continueRequested = false;
        while (!InputBindings.GetDown(GameAction.Interact) && !continueRequested) yield return null;
        continueRequested = false;
    }

    private void SetPhoneGlow(bool on)
    {
        if (phoneScreen != null) phoneScreen.enabled = on;
    }

    private void Freeze(bool on)
    {
        if (player == null) return;
        if (on)
        {
            frozen.Clear();
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

    // ---------- Text (wie Watchtower / Tankstelle) ----------
    private IEnumerator TypeIntro(string message)
    {
        introText.text = message;
        introText.maxVisibleCharacters = 0;
        introText.gameObject.SetActive(true);
        if (playerVoiceAudioSource != null && playerTalking != null)
        {
            playerVoiceAudioSource.clip = playerTalking; playerVoiceAudioSource.loop = true; playerVoiceAudioSource.Play();
        }
        var wait = new WaitForSeconds(introTypeSpeed);
        for (int i = 1; i <= message.Length; i++) { introText.maxVisibleCharacters = i; yield return wait; }
        introText.maxVisibleCharacters = int.MaxValue;
        if (playerVoiceAudioSource != null) playerVoiceAudioSource.Stop();
    }

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
            playerVoiceAudioSource.clip = playerTalking; playerVoiceAudioSource.loop = true; playerVoiceAudioSource.Play();
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
}
