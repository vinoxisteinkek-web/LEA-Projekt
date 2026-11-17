using UnityEngine;

// =====================================================
// SPRECHER
// Rechtsklick im Project-Fenster: Create > LEA > Sprecher
// Einmal pro Figur anlegen und in den Dialog-Zeilen auswählen.
// =====================================================
[CreateAssetMenu(fileName = "NewSpeaker", menuName = "LEA/Sprecher")]
public class SpeakerData : ScriptableObject
{
    [Tooltip("Name, der über dem Untertitel steht. Leer = kein Name anzeigen.")]
    public string displayName = "Name";

    [Tooltip("Farbe des Namens")]
    public Color nameColor = Color.white;

    [Header("Murmeln beim Tippen (nur wenn die Zeile KEINE Sprachaufnahme hat)")]
    [Tooltip("AUS = still (z. B. für Selbstgespräche des Spielers)")]
    public bool useTalkingSound = true;
    [Tooltip("Eigenes Murmel-Geräusch für diese Figur. Leer = Standard aus dem DialogueManager.")]
    public AudioClip talkingLoop;
}
