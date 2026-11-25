using UnityEngine;
using UnityEngine.EventSystems;

// Macht ein UI-Fenster mit der Maus verschiebbar (wird von PhoneChatUI per Code hinzugefuegt).
// Das Fenster bleibt immer komplett im Bild.
public class ChatWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
{
    public RectTransform target;

    private Canvas canvas;
    private Vector2 grabOffset;

    private void Awake()
    {
        if (target == null) target = (RectTransform)transform;
    }

    public void OnPointerDown(PointerEventData e)
    {
        // nach vorne holen, falls es mehrere Fenster gibt
        target.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        canvas = target.GetComponentInParent<Canvas>();
        RectTransform parent = (RectTransform)target.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out Vector2 local);
        grabOffset = target.anchoredPosition - local;
    }

    public void OnDrag(PointerEventData e)
    {
        RectTransform parent = (RectTransform)target.parent;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out Vector2 local))
            return;
        Vector2 pos = local + grabOffset;

        // im Bild halten
        Vector2 half = parent.rect.size * 0.5f;
        Vector2 win = target.rect.size * 0.5f;
        pos.x = Mathf.Clamp(pos.x, -half.x + win.x, half.x - win.x);
        pos.y = Mathf.Clamp(pos.y, -half.y + win.y, half.y - win.y);
        target.anchoredPosition = pos;
    }
}
