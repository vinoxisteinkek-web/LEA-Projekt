using UnityEngine;

// =====================================================
// GEGENSTAND IM HAUS (Story 2 Anfang)
// Handy, Autoschlüssel, Jacke, Auto ... -> meldet sich bei Story2HouseIntro.
// Ob man gerade damit interagieren darf, entscheidet die Story.
// =====================================================
public class HouseStoryItem : WorldInteractable
{
    public enum Kind { Phone, Keys, Jacket, Car }
    public Kind kind;
    public Story2HouseIntro story;
    [Tooltip("Nach dem Aufheben ausblenden (Schlüssel, Jacke)")]
    public bool hideOnPickup = true;

    public bool Used { get; set; }

    public override bool CanInteract(GameObject player) => !Used && story != null && story.CanUse(this);

    public override void Interact(GameObject player)
    {
        if (!CanInteract(player)) return;
        story.OnItemUsed(this);
    }
}
