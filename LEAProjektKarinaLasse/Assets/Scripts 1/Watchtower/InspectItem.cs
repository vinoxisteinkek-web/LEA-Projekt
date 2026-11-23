using UnityEngine;

// =====================================================
// UNTERSUCHBARER GEGENSTAND (Watchtower-Kabine)
// [E] Untersuchen -> der Spieler sagt die Sätze aus 'lines'.
// Jeder Gegenstand zählt einmal für "Die Kabine untersuchen".
// =====================================================
public class InspectItem : WorldInteractable
{
    public WatchtowerStory story;
    [TextArea(1, 3)] public string[] lines = { "..." };

    public bool Inspected { get; private set; }

    void Reset() { promptText = "[E] Untersuchen"; }

    public override bool CanInteract(GameObject player) => !Inspected && story != null && story.CanInspect;

    public override void Interact(GameObject player)
    {
        if (Inspected || story == null) return;
        Inspected = true;
        story.OnInspected(this);
    }
}
