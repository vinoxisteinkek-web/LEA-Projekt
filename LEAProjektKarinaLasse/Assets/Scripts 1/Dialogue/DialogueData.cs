using System;
using System.Collections.Generic;
using UnityEngine;

// =====================================================
// DIALOG-DATEN
// Ein Dialog ist ein Asset (Rechtsklick im Project-Fenster:
// Create > LEA > Dialog). Im Inspector legst du "Nodes" an.
//
// Jeder Node = eine Zeile, die jemand sagt.
//   - id:          frei wählbarer Name, z. B. "start", "frage_wetter"
//   - speaker:     wer spricht (wird über dem Text angezeigt)
//   - text:        was gesagt wird
//   - choices:     Antwortmöglichkeiten für den Spieler (optional)
//   - nextNodeId:  nur wenn es KEINE choices gibt: welcher Node kommt
//                  nach einem Klick. Leer lassen = Dialog endet.
//   - eventName:   optional, wird ausgelöst, sobald der Node angezeigt wird
//
// Jede Choice:
//   - text:        was der Spieler auswählt/sagt
//   - nextNodeId:  wohin es danach geht (leer = Dialog endet)
//   - eventName:   optional, wird beim Auswählen ausgelöst
//                  (z. B. "paid" = Spieler hat bezahlt)
// =====================================================
[CreateAssetMenu(fileName = "NewDialogue", menuName = "LEA/Dialog")]
public class DialogueData : ScriptableObject
{
    [Tooltip("Mit welchem Node der Dialog beginnt. Leer = erster Node in der Liste.")]
    public string startNodeId = "start";

    public List<DialogueNode> nodes = new List<DialogueNode>();

    public DialogueNode GetStartNode()
    {
        if (!string.IsNullOrEmpty(startNodeId))
        {
            var n = Find(startNodeId);
            if (n != null) return n;
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
}

[Serializable]
public class DialogueNode
{
    public string id = "start";
    public string speaker = "Cashier";
    [TextArea(2, 6)] public string text = "[Platzhalter]";

    [Header("Ohne Antworten: wohin geht's nach dem Klick? (leer = Ende)")]
    public string nextNodeId;

    [Header("Optional: Event beim Anzeigen")]
    public string eventName;

    [Header("Antwortmöglichkeiten (leer = einfach weiterklicken)")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();
}

[Serializable]
public class DialogueChoice
{
    [TextArea(1, 3)] public string text = "[Antwort]";
    [Tooltip("Node, der nach dieser Antwort kommt. Leer = Dialog endet.")]
    public string nextNodeId;
    [Tooltip("Optional: Event beim Auswählen, z. B. 'paid'.")]
    public string eventName;
}
