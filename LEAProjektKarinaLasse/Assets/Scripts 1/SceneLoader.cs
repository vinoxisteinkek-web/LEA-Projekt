using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// =====================================
// SZENEN LADEN MIT LADEBILDSCHIRM
// =====================================
// Statt SceneManager.LoadScene("X") einfach SceneLoader.Load("X") aufrufen.
// Laedt die Szene im Hintergrund (asynchron), zeigt solange einen
// schwarzen Bildschirm mit "Loading..." und blendet danach weich aus.
// Der Ladebildschirm wird komplett per Code erzeugt – kein Prefab noetig.
public static class SceneLoader
{
    private static bool isLoading;

    public static void Load(string sceneName)
    {
        if (isLoading)
            return;

        isLoading = true;

        GameObject go = new GameObject("SceneLoader");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<SceneLoaderRunner>().Begin(sceneName);
    }

    internal static void Finished()
    {
        isLoading = false;
    }

    // Falls das Spiel im Editor neu gestartet wird
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        isLoading = false;
    }
}


public class SceneLoaderRunner : MonoBehaviour
{
    private const float FadeOutDuration = 0.6f;

    private CanvasGroup group;
    private TextMeshProUGUI loadingText;

    public void Begin(string sceneName)
    {
        BuildUI();
        StartCoroutine(LoadRoutine(sceneName));
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // ueber allem anderen

        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = true;

        // Schwarzer Hintergrund
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(transform, false);
        Image img = bg.AddComponent<Image>();
        img.color = Color.black;
        RectTransform bgRect = img.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // "Loading..." unten rechts
        GameObject textGO = new GameObject("LoadingText");
        textGO.transform.SetParent(transform, false);
        loadingText = textGO.AddComponent<TextMeshProUGUI>();
        loadingText.text = "Loading";
        loadingText.fontSize = 28;
        loadingText.color = new Color(1f, 1f, 1f, 0.7f);
        loadingText.alignment = TextAlignmentOptions.BottomRight;
        loadingText.raycastTarget = false;
        RectTransform tRect = loadingText.rectTransform;
        tRect.anchorMin = new Vector2(1f, 0f);
        tRect.anchorMax = new Vector2(1f, 0f);
        tRect.pivot = new Vector2(1f, 0f);
        tRect.anchoredPosition = new Vector2(-40f, 30f);
        tRect.sizeDelta = new Vector2(400f, 60f);
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        // Einen Frame warten, damit der schwarze Bildschirm sicher sichtbar ist
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        float dotTimer = 0f;
        int dots = 0;

        while (!op.isDone)
        {
            // Punkte animieren (unabhaengig von Time.timeScale, z.B. aus dem Pausemenue)
            dotTimer += Time.unscaledDeltaTime;
            if (dotTimer > 0.4f)
            {
                dotTimer = 0f;
                dots = (dots + 1) % 4;
                loadingText.text = "Loading" + new string('.', dots);
            }

            yield return null;
        }

        // Neue Szene ist da – kurz warten, bis sie sich eingerichtet hat
        yield return null;
        yield return null;

        loadingText.gameObject.SetActive(false);

        float time = 0f;
        while (time < FadeOutDuration)
        {
            time += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(time / FadeOutDuration);
            yield return null;
        }

        SceneLoader.Finished();
        Destroy(gameObject);
    }
}
