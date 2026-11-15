using TMPro;
using UnityEngine;

// =====================================================
// SPIELER-SEITE DER INTERAKTION
// Liegt auf dem Spieler. Schaut, worauf die Kamera zeigt,
// zeigt den Hinweis ("[E] Tanken") und löst Interact() aus.
// =====================================================
public class PlayerInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public float reach = 2.8f;
    public KeyCode key = KeyCode.E;
    public TextMeshProUGUI promptText;

    private WorldInteractable current;
    private bool holding;

    void Update()
    {
        var dm = DialogueManager.Instance;
        if (playerCamera == null || (dm != null && (dm.IsOpen || dm.ClosedFrame == Time.frameCount)))
        {
            SetTarget(null);
            return;
        }

        SetTarget(FindTarget());
        if (current == null) return;

        if (current.holdDuration <= 0f)
        {
            ShowPrompt(current.GetPrompt());
            if (Input.GetKeyDown(key)) current.Interact(gameObject);
            return;
        }

        // E halten
        if (Input.GetKey(key))
        {
            holding = true;
            current.holdProgress = Mathf.Clamp01(current.holdProgress + Time.deltaTime / current.holdDuration);
            current.OnHold(current.holdProgress);
            ShowPrompt(current.GetPrompt() + "\n<size=80%>" + Bar(current.holdProgress) + "</size>");
            if (current.holdProgress >= 1f)
            {
                holding = false;
                var t = current;
                t.Interact(gameObject);
                t.holdProgress = 0f;
            }
        }
        else
        {
            if (holding) { holding = false; current.OnHoldReleased(); if (!current.keepHoldProgress) current.holdProgress = 0f; }
            string extra = current.holdProgress > 0f ? "\n<size=80%>" + Bar(current.holdProgress) + "</size>" : "";
            ShowPrompt(current.GetPrompt() + extra);
        }
    }

    private WorldInteractable FindTarget()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        var hits = Physics.RaycastAll(ray, reach, ~0, QueryTriggerInteraction.Collide);
        float best = float.MaxValue;
        WorldInteractable found = null;
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            var wi = h.collider.GetComponentInParent<WorldInteractable>();
            if (wi == null)
            {
                // ein fester (nicht-Trigger) Collider ohne Interaktion verdeckt alles dahinter
                if (!h.collider.isTrigger && h.distance < best) { best = h.distance; found = null; }
                continue;
            }
            if (!wi.enabled || !wi.CanInteract(gameObject)) continue;
            if (h.distance < best) { best = h.distance; found = wi; }
        }
        return found;
    }

    private void SetTarget(WorldInteractable t)
    {
        if (t == current) { if (t == null) HidePrompt(); return; }
        if (current != null && holding) { current.OnHoldReleased(); holding = false; }
        current = t;
        if (current == null) HidePrompt();
    }

    private void ShowPrompt(string s)
    {
        if (promptText == null) return;
        if (promptText.text != s) promptText.text = s;
        if (!promptText.gameObject.activeSelf) promptText.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptText != null && promptText.gameObject.activeSelf) promptText.gameObject.SetActive(false);
    }

    private static string Bar(float t)
    {
        int n = 20, f = Mathf.RoundToInt(t * n);
        return "[" + new string('|', f) + new string('.', n - f) + "] " + Mathf.RoundToInt(t * 100f) + "%";
    }

    void OnDisable()
    {
        if (current != null && holding) current.OnHoldReleased();
        holding = false;
        current = null;
        HidePrompt();
    }
}
