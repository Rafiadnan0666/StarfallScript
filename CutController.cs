using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CutController : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] audioClips;
    public string[] tulisanPerClip;
    public Text text;
    public Transform[] virtualCamPositions; 
    public Button nextButton;
    public Button skipButton; 
    public Text progressText; 
    public AudioClip backgroundMusic;
    public float cameraZoomAmount = 1.5f; 

    public Image fadeImage; 
    public float fadeDuration = 1f; 

    public Camera mainCamera;
    public float transitionDuration = 1f; 

    [SerializeField] private string ScenNext;

    private int currentSceneIndex = 0;
    private bool[] hasClipPlayed; 

    void Start()
    {
        hasClipPlayed = new bool[audioClips.Length]; // Initialize the array
        nextButton.onClick.AddListener(OnNextButtonClicked);
        skipButton.onClick.AddListener(OnSkipButtonClicked); // Add listener for skip button
        DeactivateCamera();
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        if (fadeImage == null) Debug.LogError("fadeImage is not assigned.");
        if (mainCamera == null) Debug.LogError("mainCamera is not assigned.");
        if (audioSource == null) Debug.LogError("audioSource is not assigned.");
        if (text == null) Debug.LogError("text is not assigned.");
        if (tulisanPerClip == null || tulisanPerClip.Length == 0) Debug.LogError("tulisanPerClip is empty.");
        if (virtualCamPositions == null || virtualCamPositions.Length == 0) Debug.LogError("virtualCamPositions is empty.");
        // Play background music if assigned
        if (backgroundMusic != null)
        {
            AudioSource bgMusicSource = gameObject.AddComponent<AudioSource>();
            bgMusicSource.clip = backgroundMusic;
            bgMusicSource.loop = true;
            bgMusicSource.Play();
        }

        UpdateScene();
    }

    void OnNextButtonClicked()
    {
        if (currentSceneIndex < audioClips.Length - 1)
        {
            currentSceneIndex++;
            UpdateScene();
        }
        else
        {
            StartCoroutine(FadeInBeforeSceneChange(ScenNext));
        }
    }

    IEnumerator FadeInBeforeSceneChange(string sceneName)
{
    yield return StartCoroutine(Fade(0f)); // Ensure screen is fully visible (fade in first)

    yield return new WaitForSeconds(1f); // Optional delay for smooth transition

    yield return StartCoroutine(Fade(1f)); // Now fade out before changing scene

    SceneManager.LoadScene(sceneName); // Load the new scene

    yield return new WaitForSeconds(0.1f); // Ensure scene is loaded

    yield return StartCoroutine(Fade(0f)); // Fade back in after loading
}


    void OnSkipButtonClicked()
    {
        StopAllCoroutines(); // Stop any ongoing coroutines
        audioSource.Stop(); // Stop the current audio clip
        text.text = ""; // Clear the text
        OnNextButtonClicked(); // Move to the next scene
    }

    void UpdateScene()
    {
        UpdateProgressText(); // Update the progress indicator
        StartCoroutine(PlayScene());
    }

    void UpdateProgressText()
    {
        if (progressText != null)
        {
            progressText.text = $"Scene {currentSceneIndex + 1}/{audioClips.Length}";
        }
    }

    IEnumerator PlayScene()
    {
        yield return StartCoroutine(SmoothTransitionTo(virtualCamPositions[currentSceneIndex]));

        // Fade in text
        yield return StartCoroutine(FadeText(text, 0.5f, true));

        text.text = tulisanPerClip[currentSceneIndex];
        AudioClip clip = audioClips[currentSceneIndex];

        if (clip != null && !hasClipPlayed[currentSceneIndex])
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.PlayOneShot(clip);
            hasClipPlayed[currentSceneIndex] = true;
            yield return new WaitForSeconds(clip.length);
        }

        // Fade out text
        yield return StartCoroutine(FadeText(text, 0.5f, false));
        text.text = "";
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        yield return StartCoroutine(Fade(1f)); // Fade to black (255)

        SceneManager.LoadScene(sceneName);

        yield return new WaitForSeconds(0.1f); // Ensure scene is loaded

        yield return StartCoroutine(Fade(0f)); // Fade back in (0)
    }


    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeImage.color.a;
        float timeElapsed = 0f;

        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, timeElapsed / fadeDuration);
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, targetAlpha);
    }


    IEnumerator SmoothTransitionTo(Transform targetTransform)
    {
        mainCamera.gameObject.SetActive(true);
        float elapsedTime = 0f;
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        float startFOV = mainCamera.fieldOfView;
        float targetFOV = startFOV / cameraZoomAmount; // Zoom in for dramatic effect

        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetTransform.position, t);
            mainCamera.transform.rotation = Quaternion.Lerp(startRotation, targetTransform.rotation, t);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t); // Smoothly zoom in
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset FOV after transition
        mainCamera.fieldOfView = startFOV;
        mainCamera.transform.position = targetTransform.position;
        mainCamera.transform.rotation = targetTransform.rotation;
    }

    IEnumerator FadeText(Text textElement, float duration, bool fadeIn)
    {
        float elapsedTime = 0f;
        Color textColor = textElement.color;
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;

        textElement.color = new Color(textColor.r, textColor.g, textColor.b, startAlpha);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            textElement.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
            yield return null;
        }

        textElement.color = new Color(textColor.r, textColor.g, textColor.b, endAlpha);
    }

    void DeactivateCamera()
    {
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(false);
        }
    }
}






