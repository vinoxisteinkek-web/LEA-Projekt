using UnityEngine;

// =====================================================
// BODEN-MARKIERUNG FÜR SCHRITTE (optional)
// Normalerweise erkennt der PlayerController den Boden selbst
// am Material-/Objektnamen (z. B. "Wood" -> Holz, "Grass" -> Gras).
// Passt das irgendwo nicht, dieses Script auf den Boden legen
// und die Art von Hand festlegen.
// =====================================================
public class FootstepSurface : MonoBehaviour
{
    public enum Type { Standard, Wood, Grass }
    public Type surface = Type.Wood;
}
