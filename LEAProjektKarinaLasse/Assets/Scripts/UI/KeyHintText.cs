using TMPro;
using UnityEngine;

// =====================================
// HINWEISTEXT MIT AKTUELLER TASTE
// =====================================
// Auf ein TextMeshPro-Objekt legen. Platzhalter im Template werden
// durch die aktuell belegte Taste ersetzt, z.B.
//   "Press {Flashlight} for flashlight"  ->  "Press F for flashlight"
// Moegliche Platzhalter: {MoveForward} {MoveBackward} {MoveLeft} {MoveRight}
// {Interact} {Drop} {Use} {Flashlight} {Handbrake}
[RequireComponent(typeof(TMP_Text))]
public class KeyHintText : MonoBehaviour
{
    [TextArea]
    [SerializeField] private string template = "Press {Flashlight} for flashlight";

    private TMP_Text text;

    private void OnEnable()
    {
        text = GetComponent<TMP_Text>();
        InputBindings.OnBindingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        InputBindings.OnBindingsChanged -= Refresh;
    }

    private void Refresh()
    {
        if (text == null)
            return;

        string s = template;
        foreach (GameAction a in InputBindings.AllActions)
            s = s.Replace("{" + a + "}", InputBindings.GetKeyName(a));

        text.text = s;
    }
}
