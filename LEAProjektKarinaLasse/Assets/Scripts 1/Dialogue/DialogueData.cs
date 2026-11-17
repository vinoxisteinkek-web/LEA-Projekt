using System;
using System.Collections.Generic;
using UnityEngine;

// =====================================================
// DIALOG-DATEN
// Ein Dialog ist ein Asset (Rechtsklick im Project-Fenster:
// Create > LEA > Dialog). Im Inspector legst du "Nodes" an.
//
// REIHENFOLGE: Die Zeilen laufen einfach von oben nach unten ab.
// Mit den Pfeilen / Drag & Drop in der Liste änderst du die Reihenfolge.
//
// Jeder Node = eine Zeile, die jemand sagt.
//   - id:            nur nötig, wenn eine Antwort oder ein anderer Node
//                    hierher springen soll (z. B. "ja", "nein")
//   - speakerData:   Sprecher-Asset (Create > LEA > Sprecher) -> Name + Farbe
//   - speaker:       alternativ einfach den Namen eintippen
//   - text:          was gesagt wird
//   - voiceClip:     Sprachaufnahme / Sound für diese Zeile (Voice Acting)
//   - advance:       wie es weitergeht: Taste / nach dem Audio / nach Sekunden
//   - nextNodeId:    leer = nächste Zeile in der Liste
//                    END  = Dialog endet hier
//                    id   = springt zu diesem Node
//   - eventName:     optional, wird ausgelöst, sobald die Zeile startet
//   - choices:       Antwortmöglichkeiten (dann wartet der Dialog auf eine Auswahl)
//
// Jede Choice:
//   - text:          was der Spieler auswählt/sagt
//   - nextNodeId:    wohin es danach geht (leer oder END = Dialog endet)
//   - eventName:     optional, wird beim Auswählen ausgelöst (z. B. "paid")
// =====================================================
[CreateAssetMenu(fileName = "NewDialogue", menuName = "LEA/Dialog")]
public class DialogueData : ScriptableObject
{
    [Header("Einstellungen")]
    [Tooltip("AN = Spieler kann sich während des Dialogs nicht bewegen/umschauen.\nAUS = z. B. Selbstgespräch beim Autofahren (weiter mit Interact-Taste oder automatisch).")]
    public bool lockPlayer = true;

    [Tooltip("Lautstärke von Atmo/Musik während des Dialogs (1 = normal, 0 = stumm).\nWirkt auf alle AudioSources mit der Komponente 'DialogueDucking'.")]
    [Range(0f, 1f)] public float ambienceVolumeDuringDialogue = 0.35f;

    [Tooltip("Mit welchem Node der Dialog beginnt. Leer = erste Zeile der Liste.")]
    public string startNodeId = "";

    public List<DialogueNode> nodes = new List<DialogueNode>();

    public DialogueNode GetStartNode()
    {
        if (!string.IsNullOrEmpty(startNodeId))
        {
            foreach (var n in nodes)
                if (n.id == startNodeId) return n;
        }
        return nodes.Count > 0 ? nodes[0] : null;
    }

    public DialogueNode Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var n in nodes)
            if (n.id == id) return n;
        Debug.LogWarning($"Dialog '{name}': Node mit id '{id}' nicht gefunden.", this);
        return null;
    }

    // Die Zeile, die in der Liste direkt darunter steht (null = keine mehr)
    public DialogueNode GetNext(DialogueNode node)
    {
        int i = nodes.IndexOf(node);
        return (i >= 0 && i + 1 < nodes.Count) ? nodes[i + 1] : null;
    }
}

public enum DialogueAdvance
{
    [InspectorName("Taste drücken")] Key,
    [InspectorName("Automatisch nach dem Audio")] AfterAudio,
    [InspectorName("Automatisch nach Sekunden")] AfterSeconds
}

[Serializable]
public class DialogueNode
{
    [Tooltip("Nur nötig, wenn eine Antwort oder ein anderer Node hierher springen soll.")]
    public string id = "";

    [Header("Wer spricht?")]
    [Tooltip("Sprecher-Asset (Name + Farbe). Hat Vorrang vor dem Textfeld darunter.")]
    public SpeakerData speakerData;
    [Tooltip("Alternativ: Name einfach eintippen (leer = kein Name).")]
    public string speaker = "";

    [Header("Was wird gesagt?")]
    [TextArea(2, 6)] public string text = "";

    [Header("Sound / Voice Acting")]
    [Tooltip("Sprachaufnahme für diese Zeile. Leer = Standard-Murmeln beim Tippen.")]
    public AudioClip voiceClip;
    [Range(0f, 1f)] public float voiceVolume = 1f;

    [Header("Ablauf")]
    [Tooltip("Pause in Sekunden, bevor die Zeile erscheint (gut für Spannung).")]
    public float delayBefore = 0f;
    public DialogueAdvance advance = DialogueAdvance.Key;
    [Tooltip("Nur bei 'Automatisch nach Sekunden': so lange bleibt die Zeile stehen (ab fertig getippt).")]
    public float seconds = 3f;
    [Tooltip("Nur bei 'Automatisch nach dem Audio': kurze Pause nach dem Audio.")]
    public float pauseAfterAudio = 0.4f;

    [Tooltip("Leer = nächste Zeile in der Liste.  END = Dialog endet.  Sonst die id eines Nodes.")]
    public string nextNodeId = "";

    [Header("Optional: Event beim Anzeigen")]
    public string eventName;

    [Header("Antwortmöglichkeiten (leer = keine Auswahl)")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();
}

[Serializable]
public class DialogueChoice
{
    [TextArea(1, 3)] public string text = "[Antwort]";
    [Tooltip("Node, der nach dieser Antwort kommt. Leer oder END = Dialog endet.")]
    public string nextNodeId;
    [Tooltip("Optional: Event beim Auswählen, z. B. 'paid'.")]
    public string eventName;
}
