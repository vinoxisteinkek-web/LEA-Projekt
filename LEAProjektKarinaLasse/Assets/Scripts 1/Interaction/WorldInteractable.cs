using UnityEngine;

// =====================================================
// BASIS FÜR ALLES, WOMIT MAN PER BLICK + [E] INTERAGIERT
// Auf ein Objekt mit Collider legen (auch Trigger geht).
// holdDuration > 0  = E muss gehalten werden (z. B. Tanken).
// Eigene Interaktionen: von dieser Klasse erben und Interact() füllen.
// =====================================================
public abstract class WorldInteractable : MonoBehaviour
{
    [Header("Interaktion")]
    public string promptText = "[E] Interagieren";
    [Tooltip("0 = einmal drücken, sonst Sekunden, die E gehalten werden muss")]
    public float holdDuration = 0f;
    [Tooltip("Fortschritt bleibt erhalten, wenn man loslässt")]
    public bool keepHoldProgress = true;

    [HideInInspector] public float holdProgress; // 0..1

    public virtual bool CanInteract(GameObject player) => true;
    public virtual string GetPrompt() => promptText;

    // Wird jeden Frame aufgerufen, solange E gehalten wird (t = 0..1)
    public virtual void OnHold(float t) { }
    public virtual void OnHoldReleased() { }

    public abstract void Interact(GameObject player);
}
