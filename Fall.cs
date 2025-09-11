using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Fall : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public GameObject Camera;
    public Rigidbody Pod;
    public GameObject canvasUI;
    public Door doorScript;
    public GameObject GroundChechk;

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeDuration = 2f;

    [Header("Player Spawn Settings")]
    [Tooltip("Set this to the child object attached to the pod which indicates where to move the player after landing.")]
    public Transform playerSpawnPoint;


    [SerializeField] public bool isLanded = false;
    [SerializeField] public bool isActivated = false;

    void Start()
    {
        if (player != null)
            player.SetActive(false);

        if (fadeImage != null)
            StartCoroutine(FadeOut());

        if (Pod != null)
        {
            Pod.isKinematic = true;
            Pod.useGravity = false;
        }

        if (Camera != null)
            Camera.SetActive(false);

        if (canvasUI != null)
            canvasUI.SetActive(false);
    }

    IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        float elapsedTime = 0f;
        Color imageColor = fadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            imageColor.a = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
            fadeImage.color = imageColor;
            yield return null;
        }

        fadeImage.gameObject.SetActive(false);

        if (player != null)
            player.SetActive(true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("Ground Checker Activated!");

           
            AudioSource landingSound = GetComponent<AudioSource>();
            if (landingSound != null)
                landingSound.Play();

            ParticleSystem landingParticles = GetComponent<ParticleSystem>();
            if (landingParticles != null)
                landingParticles.Play();

            LandPod(); 
        }
    }

    public void ActivatePod()
    {
        if (isActivated)
            return;

        isActivated = true;

        if (Pod != null)
        {
            Pod.isKinematic = false;
            Pod.useGravity = true;
        }

        if (Camera != null)
            Camera.SetActive(true);

        if (player != null)
            player.SetActive(false);

        StartCoroutine(WaitForLanding());
    }

 

    IEnumerator WaitForLanding()
    {
        while (!isLanded)
        {
            yield return null; // Wait until the pod lands.
        }
        LandPod();
    }

    public void LandPod()
    {
        Debug.Log("Pod has landed!");

        // Activate UI and enable any related door mechanics.
        if (canvasUI != null)
            canvasUI.SetActive(true);

        if (doorScript != null)
            doorScript.enabled = true;

        if (Camera != null)
            Camera.SetActive(false);

        // Update the player's position and enable them after landing.
        if (player != null)
        {
            if (playerSpawnPoint != null)
            {
                Debug.Log("Moving player to spawn point.");
                player.transform.position = playerSpawnPoint.position;
                player.transform.rotation = playerSpawnPoint.rotation;
            }
            player.SetActive(true);
        }
    }
}


