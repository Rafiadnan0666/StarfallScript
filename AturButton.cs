using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AturButton : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;
    private bool isTransitioning = false;

    void Start()
    {
        if (fadeImage != null)
        {
            // Ensure the fade image is fully transparent at start
            Color startColor = fadeImage.color;
            startColor.a = 0f;
            fadeImage.color = startColor;
        }
    }

    public void StartG()
    {
        LoadSceneWithFade("level 1");
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void MainMenu()
    {
        LoadSceneWithFade("mainmenu");
    }

    public void Retry()
    {
        LoadSceneWithFade(SceneManager.GetActiveScene().name);
    }

    private void LoadSceneWithFade(string sceneName)
    {
        if (!isTransitioning)
            StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        isTransitioning = true;

        // Fade to black
        if (fadeImage != null)
            yield return StartCoroutine(FadeToAlpha(1f));

        // Load scene async
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
            yield return null;
    }

    private IEnumerator FadeToAlpha(float targetAlpha)
    {
        if (fadeImage == null) yield break;

        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            Color color = fadeImage.color;
            color.a = newAlpha;
            fadeImage.color = color;
            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
    }
}
