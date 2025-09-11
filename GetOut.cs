using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetOut : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public GameObject Camera;
    public Rigidbody Pod;
    public GameObject canvasUI;
    public Door doorScript;
    public GameObject GroundChechk;

    [Header("Fade Settings")]
    public float fadeDuration = 2f;

    [Header("Player Spawn Settings")]
    [Tooltip("Set this to the child object attached to the pod which indicates where to move the player after landing.")]
    public Transform playerSpawnPoint;
    public Transform cameraSpawnPoint;
    public Transform ExtractSpawnPoint;

    [SerializeField] public bool isLanded = false;
    [SerializeField] public bool isActivated = false;



    public void ActivateGetOut()
    {
        // Optionally check for isLanded if needed
        // if (!isLanded) return;

        if (player != null)
            player.SetActive(false);

        if (Camera != null && cameraSpawnPoint != null)
        {
            Camera.SetActive(true);
            Camera.transform.position = cameraSpawnPoint.position;
            Camera.transform.rotation = cameraSpawnPoint.rotation;
        }

        if (ExtractSpawnPoint != null)
        {
            StartCoroutine(LerpToExtractPoint());
        }
    }

    private IEnumerator LerpToExtractPoint()
    {
        float duration = 2f;
        float elapsed = 0f;
        Vector3 start = transform.position;
        Vector3 end = ExtractSpawnPoint.position;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = end;
        //if already on designated area spawn player on spawn point
        if (playerSpawnPoint != null)
        {
            player.transform.position = playerSpawnPoint.position;
            player.transform.rotation = playerSpawnPoint.rotation;
            player.SetActive(true);
        }
    }




}
