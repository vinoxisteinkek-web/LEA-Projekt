using UnityEngine;
using TMPro;

public class StoryMessageScript : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;

    public void ShowMessage(string message)
    {
        dialogueText.text = message;
        dialogueText.gameObject.SetActive(true);
    }

    public void HideMessage()
    {
        dialogueText.gameObject.SetActive(false);
    }
}