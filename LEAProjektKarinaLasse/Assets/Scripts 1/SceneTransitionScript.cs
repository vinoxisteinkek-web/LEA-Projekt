using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionScript : MonoBehaviour
{
    public CanvasGroup blackScreen;

    public float fadeDuration = 2f;

    private bool isTransitioning = false;

    private void Start()
    {
        // Am Anfang soll der Bildschirm durchsichtig sein
        blackScreen.alpha = 0f;
        blackScreen.gameObject.SetActive(true);

        // Warten, bis die nächste Szene geladen wurde
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTransitioning)
            return;

        if (other.CompareTag("Player"))
        {
            isTransitioning = true;

            StartCoroutine(ChangeScene());
        }
    }

    private IEnumerator ChangeScene()
    {
        // Langsam schwarz werden
        yield return StartCoroutine(FadeToBlack());

        // Supermarkt-Szene laden
        SceneManager.LoadScene("SupermarketScene");
    }

    private IEnumerator FadeToBlack()
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            blackScreen.alpha = Mathf.Lerp(
                0f,
                1f,
                time / fadeDuration
            );

            yield return null;
        }

        blackScreen.alpha = 1f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Nur reagieren, wenn die Supermarkt-Szene geladen wurde
        if (scene.name == "SupermarketScene")
        {
            StartCoroutine(FadeFromBlack());
        }
    }

    private IEnumerator FadeFromBlack()
    {
        // Kurz komplett schwarz bleiben
        yield return new WaitForSeconds(0.5f);

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            blackScreen.alpha = Mathf.Lerp(
                1f,
                0f,
                time / fadeDuration
            );

            yield return null;
        }

        blackScreen.alpha = 0f;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}