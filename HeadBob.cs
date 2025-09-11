using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadbobSystem : MonoBehaviour
{
    [Range(0.001f, 0.01f)] public float WalkAmount = 0.002f;
    [Range(1f, 30f)] public float WalkFrequency = 10.0f;

    [Range(0.001f, 0.02f)] public float RunAmount = 0.004f;
    [Range(1f, 40f)] public float RunFrequency = 20.0f;

    [Range(0.001f, 0.005f)] public float CrouchAmount = 0.001f;
    [Range(1f, 15f)] public float CrouchFrequency = 6.0f;

    [Range(10f, 100f)] public float Smooth = 10.0f;

    private Vector3 startPosition;
    private float currentAmount;
    private float currentFrequency;

    private Player playerMovement;

    void Start()
    {
        startPosition = transform.localPosition;

        // Get reference to player movement script
        playerMovement = GetComponentInParent<Player>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement script not found in parent!");
        }
    }

    void Update()
    {
        if (playerMovement != null)
        {
            //UpdateHeadbobParameters();
            HandleHeadbob();
        }
    }

    //private void UpdateHeadbobParameters()
    //{
    //    // Adjust head bob parameters based on player's state
    //    if (playerMovement.IsRunning)
    //    {
    //        currentAmount = RunAmount;
    //        currentFrequency = RunFrequency;
    //    }
    //    else if (playerMovement.IsCrouching)
    //    {
    //        currentAmount = CrouchAmount;
    //        currentFrequency = CrouchFrequency;
    //    }
    //    else if (playerMovement.IsWalking)
    //    {
    //        currentAmount = WalkAmount;
    //        currentFrequency = WalkFrequency;
    //    }
    //    else
    //    {
    //        currentAmount = 0;
    //        currentFrequency = 0;
    //    }
    //}

    private void HandleHeadbob()
    {
        float inputMagnitude = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).magnitude;

        if (inputMagnitude > 0)
        {
            Vector3 pos = Vector3.zero;
            pos.y += Mathf.Lerp(pos.y, Mathf.Sin(Time.time * currentFrequency) * currentAmount * 1.4f, Smooth * Time.deltaTime);
            pos.x += Mathf.Lerp(pos.x, Mathf.Cos(Time.time * currentFrequency / 2f) * currentAmount * 1.6f, Smooth * Time.deltaTime);
            transform.localPosition += pos;
        }
        else
        {
            // Reset to start position when not moving
            transform.localPosition = Vector3.Lerp(transform.localPosition, startPosition, Smooth * Time.deltaTime);
        }
    }
}
