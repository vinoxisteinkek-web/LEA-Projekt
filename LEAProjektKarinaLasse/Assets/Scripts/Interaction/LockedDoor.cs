using UnityEngine;
using TMPro;
using System.Collections;

public class LockedDoor : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Settings")]
    [SerializeField] private float messageDuration = 2f;

    private Coroutine messageCoroutine;

    public void Interact()
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        messageCoroutine = StartCoroutine(ShowMessage());
    }

    private IEnumerator ShowMessage()
    {
        messageText.text = "for some strange reason the door was locked";
        messageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(messageDuration);

        messageText.gameObject.SetActive(false);

        messageCoroutine = null;
    }
}