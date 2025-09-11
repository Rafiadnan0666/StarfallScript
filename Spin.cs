using System.Collections;
using UnityEngine;

public class Spin : MonoBehaviour
{
    public bool isSpinning = false; // Flag to check if spinning is active
    public GameObject[] fakeGuns; // Array of fake gun prefabs
    public GameObject[] realGuns; // Array of real gun prefabs
    public Transform spawnPoint; // The point where guns spawn
    public Transform player; // Reference to the player's transform
    private GameObject currentSpinGun; // The currently spawned fake gun
    private int currentGunIndex = -1; // Index of the currently spinning gun
    public AudioSource audioSource;
    public AudioClip Muter;
    public AudioClip FinalSpin;
    private float spinDuration = 10f; // Duration of the spinning process
    public bool OnceSpin = false; // Flag to ensure the spin occurs only once
    public GameObject Exp;
    public GameObject Pintu;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void ToggleSpin()
    {
        if (!isSpinning && !OnceSpin)
        {
            isSpinning = true;
            StartCoroutine(SpinGunsCoroutine());
        }
    }

    private IEnumerator SpinGunsCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < spinDuration)
        {
            currentGunIndex = Random.Range(0, fakeGuns.Length); // Get a random fake gun index
            Debug.LogWarning(currentGunIndex);

            // Spawn a new fake gun if one isn't already spinning
            if (currentSpinGun != null)
            {
                Destroy(currentSpinGun);
            }

            currentSpinGun = Instantiate(fakeGuns[currentGunIndex], spawnPoint.position, spawnPoint.rotation);

            // Rotate the fake gun continuously while spinning
            if (currentSpinGun != null)
            {
                currentSpinGun.transform.Rotate(Vector3.up * 360 * Time.deltaTime, Space.Self);
            }

            if (audioSource != null && Muter != null)
            {
                audioSource.PlayOneShot(Muter);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
            
        }

        // After spinning, spawn and throw the real gun
        SpawnAndThrowRealGun();

        // Clean up fake gun
        if (currentSpinGun != null)
        {
            Destroy(currentSpinGun);
            currentSpinGun = null;
        }

        isSpinning = false; 
    }

    private void SpawnAndThrowRealGun()
    {
        if (!OnceSpin)
        {
            if (currentGunIndex < 0 || currentGunIndex >= realGuns.Length)
            {
                Debug.LogError("Invalid gun index.");
                return;
            }

            OnceSpin = true;

            // Instantiate the real gun
            GameObject realGun = Instantiate(realGuns[currentGunIndex], spawnPoint.position, spawnPoint.rotation);

            // Add force to throw the gun towards the player
            Rigidbody rb = realGun.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (player.position - spawnPoint.position).normalized; // Direction towards the player
                rb.AddForce(direction * 50f); // Adjust the force multiplier as needed
                rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, 20f); // Limit velocity
            }

            if (audioSource != null && FinalSpin != null)
            {
                audioSource.PlayOneShot(FinalSpin);
            }

            // Start door opening animation
            StartCoroutine(OpenDoor());
        }
        else
        {
            Instantiate(Exp, transform.position, Quaternion.identity);
        }
    }

    private IEnumerator OpenDoor()
    {
        Vector3 targetPosition = new Vector3(Pintu.transform.position.x,
                                             Pintu.transform.position.y + 10,
                                             Pintu.transform.position.z);
        Vector3 initialPosition = Pintu.transform.position;
        float elapsedTime = 0f;
        float duration = 2f; // Adjust the duration as needed

        while (elapsedTime < duration)
        {
            Pintu.transform.position = Vector3.Lerp(initialPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Pintu.transform.position = targetPosition;
    }
}
