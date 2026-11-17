using UnityEngine;

// =====================================================
// ATMO / MUSIK LEISER BEIM REDEN
// Auf jede AudioSource legen, die während eines Dialogs leiser
// werden soll (Atmo, Musik, Radio ...). Wie leise, steht im
// Dialog-Asset ("Ambience Volume During Dialogue").
// Die normale Lautstärke stellst du wie gewohnt an der AudioSource ein.
// =====================================================
[RequireComponent(typeof(AudioSource))]
public class DialogueDucking : MonoBehaviour
{
    [Tooltip("Wie schnell leiser/lauter geblendet wird (pro Sekunde)")]
    public float fadeSpeed = 2f;

    private AudioSource source;
    private float baseVolume;
    private float factor = 1f;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        baseVolume = source.volume;
    }

    void Update()
    {
        var dm = DialogueManager.Instance;
        float target = dm != null ? dm.DuckTarget : 1f;
        factor = Mathf.MoveTowards(factor, target, fadeSpeed * Time.unscaledDeltaTime);
        source.volume = baseVolume * factor;
    }

    // Falls ein anderes Script die Lautstärke ändern will, dies benutzen
    public void SetBaseVolume(float volume) => baseVolume = volume;

    void OnDisable()
    {
        if (source != null) source.volume = baseVolume;
        factor = 1f;
    }
}
