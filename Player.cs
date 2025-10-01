using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.VisualScripting;
using InfimaGames.LowPolyShooterPack;
using Starfall;
using System;
using TMPro;

public class Player : MonoBehaviour
{
    // Player Stats
    public float health;
    public float speed;
    public float maxHealth;
    public float stamina;
    public float maxStamina;
    public float minStamina;

    public float xFrequency = 4.1f;
    public float xAmplitude = 1.0f;
    public float yFrequency = 4.3f;
    public float yAmplitude = 1.0f;

    public TextMeshProUGUI missionNameText;
    public TextMeshProUGUI forPlayer;

    // UI
    public Text healthText;
    public Image imehealth;
    public Image staminaBar;
    public Image speedIndicator;
    public TextMeshProUGUI info;
    public Canvas canvasMain;
    public Canvas canvasPause;
    public Canvas canvasMati;

    // Assignables
    public Transform playerCam;
    public Transform orientation;
    public GameObject forceFieldPrefab;

    // Other
    private Rigidbody rb;
    private bool isBoosting = false;
    private Vector3 normalVector = Vector3.up;
    private float damage;

    // Rotation and look
    private float xRotation;
    private float yRotation;
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;

    [SerializeField] private Transform PlayerBody;
    [SerializeField] public Transform mainCamera;

    [SerializeField] public Camera playerCamera;

    [SerializeField] private Transform RightHandTarget;
    [SerializeField] private Transform LeftHandTarget;

    // Movement
    public float walkSpeed = 7f;
    public float runSpeed = 15f;
    public float boostSpeed = 20f;
    public float maxSpeed = 20f;
    public bool grounded = false;
    public LayerMask whatIsGround;
    private bool isSliding = false;

    //planet stat
    public TerrainGenerator getor;
    //public DeploymentOff deployoff;

    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;

    // Crouch & Slide
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    private Vector3 originalChildrenPosition;
    private bool isCrouching = false;

    // Jumping & Bunny Hop
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public float jumpForce = 550f;
    private int jumpCount = 0;
    private int maxJumpCount = 2;
    private float lastJumpTime = 0f;
    private float jumpBufferTime = 0.15f;
    private float coyoteTime = 0.1f;
    private float lastGroundedTime = 0f;

    // Camera Shake
    private float walkShakeAmount = 0.02f;
    private float runShakeAmount = 0.08f;
    private float shakeFrequency = 1.5f;
    private Vector3 initialCamPosition;

    // Input
    private float x, y;
    public bool jumping, crouching, sprinting;

    // Step Sound
    public AudioClip stepSound;
    public AudioClip Drop;
    private AudioSource audioSource;
    private float stepInterval = 0.5f;
    private float nextStepTime = 0f;
    private bool isPlayingStepSound = false;

    //built in
    public Light Senter;
    public bool Senternya = false;
    public Transform groundCheck;
    public float groundDistance = 0.2f;

    public Transform AimPosition;

    //wwalk run jump leg
    public Transform RightJump;
    public Transform LeftJump;

    public Transform RightLegWFront;
    public Transform LeftLegWFront;

    public Transform RightLegWBack;
    public Transform LeftLegWBack;

    public Transform RightLegWStop;
    public Transform LeftLegWStop;

    public Transform Kick;

    public Transform RightLeg;
    public Transform LeftLeg;
    public GameObject Spawn;

    Vector3 previousAngle;
    float accelerationBuffer = 0.3f;
    float decelerationBuffer = 0.1f;
    private float rotationAmount;
    private float runMultiplier;

    // Movement smoothing
    private Vector2 currentInput;
    private Vector2 targetInput;
    private float inputSmoothTime = 0.1f;
    private float inputSmoothVelocityX;
    private float inputSmoothVelocityY;

    // Landing effects
    private float fallStartY;
    private bool wasFalling = false;
    public float hardLandingThreshold = 5f;

    private void Update()
    {
        MyInput();
        Look();
        SmoothCameraShake();
        PlayStepSound();
        UpdateUI();
        Die();
        HandleLegMovement();
        PlayerBody.transform.rotation = Quaternion.Euler(0, mainCamera.eulerAngles.y, 0);

        // Update grounded state with coyote time
        //if (Physics.CheckSphere(groundCheck.position, groundDistance, whatIsGround))
        //{
        //    grounded = true;
        //    lastGroundedTime = Time.time;
        //    jumpCount = 0; // Reset jump count when grounded
        //}
        //else
        //{
        //    grounded = false;
        //}
    }

    

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        playerScale = transform.localScale;
        originalChildrenPosition = transform.GetChild(0).localPosition; // Store original child position
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerCam.gameObject.SetActive(true);
        canvasPause.gameObject.SetActive(false);
        audioSource = GetComponent<AudioSource>();
        initialCamPosition = playerCam.localPosition;
        canvasMati.gameObject.SetActive(false);
        Senter.gameObject.SetActive(Senternya);
        mainCamera = playerCam;

        getor = GetComponent<TerrainGenerator>();

        grounded = false;
        damage = 10;
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet") || collision.gameObject.CompareTag("Exp"))
        {
            health -= damage;
            Destroy(collision.gameObject);
        }

        if(collision.gameObject)
        {
            grounded = true;
        }
        else
        {
                       grounded = false;
        }

        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Untagged") || collision.gameObject)
        {
            // Landing effects
            if (wasFalling)
            {
                float fallDistance = fallStartY - transform.position.y;
                if (fallDistance > hardLandingThreshold)
                {
                    // Hard landing - apply camera shake and sound
                    StartCoroutine(HardLandingEffect(fallDistance));
                }
                wasFalling = false;
            }

            if (Drop == null)
            {
                Debug.LogWarning("Drop audio clip is not assigned in the inspector.");
            }
            else
            {
                audioSource.PlayOneShot(Drop);
            }
        }
    }

    private IEnumerator HardLandingEffect(float fallDistance)
    {
        float intensity = Mathf.Clamp01(fallDistance / 10f);
        float duration = 0.3f * intensity;
        float elapsed = 0f;

        Vector3 originalPos = playerCam.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float shake = Mathf.Sin(elapsed * 30f) * intensity * (1f - elapsed / duration);
            playerCam.localPosition = originalPos + UnityEngine.Random.insideUnitSphere * shake;
            yield return null;
        }

        playerCam.localPosition = originalPos;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Untagged") || collision.gameObject)
        {
            // Start tracking fall height
            if (!wasFalling && rb.linearVelocity.y <= 0)
            {
                wasFalling = true;
                fallStartY = transform.position.y;
            }
            grounded = false;   
        }
    }

    private Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float magnitude = rb.linearVelocity.magnitude;
        float yMag = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitude * Mathf.Sin(u * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private void MyInput()
    {
        targetInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        currentInput.x = Mathf.SmoothDamp(currentInput.x, targetInput.x, ref inputSmoothVelocityX, inputSmoothTime);
        currentInput.y = Mathf.SmoothDamp(currentInput.y, targetInput.y, ref inputSmoothVelocityY, inputSmoothTime);

        x = currentInput.x;
        y = currentInput.y;

        jumping = Input.GetButton("Jump");
        crouching = Input.GetKey(KeyCode.C);
        sprinting = Input.GetKey(KeyCode.LeftShift);

        if (sprinting && stamina > minStamina)
        {
            stamina -= Time.deltaTime * 10f;
        }
        else
        {
            if (stamina < maxStamina)
            {
                stamina += Time.deltaTime * 5f;
            }
        }

        // Handle jump input with buffer
        if (Input.GetButtonDown("Jump"))
        {
            lastJumpTime = Time.time;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            jumping = true;

        }

        TogglePause();
        senter();

        if (Input.GetKeyDown(KeyCode.C))
            StartCrouch();
        if (Input.GetKeyUp(KeyCode.C))
            StopCrouch();
    }

    private void HandleLegMovement()
    {
        if (!grounded)
        {
            SmoothMoveLeg(RightLeg, RightJump.position);
            SmoothMoveLeg(LeftLeg, LeftJump.position);
        }
        else if (x != 0 || y != 0)
        {
            float speedFactor = Mathf.Clamp01(new Vector2(x, y).magnitude);
            float cycle = (Time.time * speedFactor * 2f) % 1.0f;

            Wobble();

            if (cycle < 0.5f)
            {
                SmoothMoveLeg(RightLeg, RightLegWFront.position);
                SmoothMoveLeg(LeftLeg, LeftLegWBack.position);
            }
            else
            {
                SmoothMoveLeg(RightLeg, RightLegWBack.position);
                SmoothMoveLeg(LeftLeg, LeftLegWFront.position);
            }
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            LeftLeg.position = Kick.position;
        }
        else
        {
            SmoothMoveLeg(RightLeg, RightLegWStop.position);
            SmoothMoveLeg(LeftLeg, LeftLegWStop.position);
        }
    }

    private void SmoothMoveLeg(Transform leg, Vector3 targetPosition)
    {
        float smoothSpeed = 5f;
        leg.position = Vector3.Lerp(leg.position, targetPosition, Time.deltaTime * smoothSpeed);
    }

    void Wobble()
    {
        Spawn.transform.position = new Vector3(
           Mathf.Sin(Time.time * xFrequency) * xAmplitude,
           Mathf.Sin(Time.time * yFrequency) * yAmplitude,
           0f
       );
    }

    private void StartCrouch()
    {
        isCrouching = true;

        if (sprinting && grounded)
        {
            isSliding = true;
          
            Vector3 slideDirection = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).normalized;
            rb.AddForce(slideDirection * 2f, ForceMode.VelocityChange);
            rb.AddForce(-rb.linearVelocity * 0.2f, ForceMode.Acceleration);
        }

        // Scale only the parent, not children
        transform.localScale = crouchScale;

        // Move children down slightly to compensate
        foreach (Transform child in transform)
        {
            child.localPosition += new Vector3(0, -0.25f, 0);
        }
    }

    private void StopCrouch()
    {
        isCrouching = false;
        isSliding = false;

        // Return to normal scale
        transform.localScale = playerScale;

        // Move children back to original position
        foreach (Transform child in transform)
        {
            child.localPosition = new Vector3(child.localPosition.x, originalChildrenPosition.y, child.localPosition.z);
        }
    }

    private void Movement()
    {
        float currentSpeed = isBoosting ? boostSpeed : (sprinting ? runSpeed : walkSpeed);
        Vector2 mag = FindVelRelativeToLook();
        CounterMovement(x, y, mag);

        // Handle jumping with coyote time and jump buffering
        if ((Time.time - lastGroundedTime <= coyoteTime) && (Time.time - lastJumpTime <= jumpBufferTime) && readyToJump)
        {
            Jump();
            lastJumpTime = 0; // Consume the jump input
        }

        float multiplier = grounded ? 1f : 0.5f;

        // Apply movement forces
        if (!isSliding)
        {
            rb.AddForce(orientation.transform.forward * y * currentSpeed * Time.deltaTime * multiplier);
            rb.AddForce(orientation.transform.right * x * currentSpeed * Time.deltaTime * multiplier);
        }

        // Limit horizontal velocity
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            if (horizontalVel.magnitude > maxSpeed)
            {
                horizontalVel = horizontalVel.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);
            }
        }
    }

    private void Jump()
    {
        if ((grounded || Time.time - lastGroundedTime <= coyoteTime || jumpCount < maxJumpCount) && readyToJump)
        {
            readyToJump = false;

            // Reset vertical velocity for consistent jump height
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            rb.AddForce(Vector3.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            jumpCount++;
            lastGroundedTime = 0; // Reset coyote time

            // Apply a small forward force when bunny hopping
            if (jumpCount > 1 && (x != 0 || y != 0))
            {
                Vector3 hopDirection = orientation.transform.forward * y + orientation.transform.right * x;
                rb.AddForce(hopDirection.normalized * jumpForce * 0.2f);
            }

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        xRotation = Mathf.Clamp(xRotation - mouseY, -70f, 55f);
        yRotation += mouseX;

        playerCam.localRotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.localRotation = Quaternion.Euler(0, yRotation, 0);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping) return;

        // Counter movement for more responsive controls
        if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(x) < 0.05f)
            rb.AddForce(orientation.transform.right * -mag.x * counterMovement);
        if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(y) < 0.05f)
            rb.AddForce(orientation.transform.forward * -mag.y * counterMovement);

        // Extra gravity for better feel
        if (rb.linearVelocity.y < 0)
        {
            rb.AddForce(Vector3.down * 10f);
        }
    }

    private void SmoothCameraShake()
    {
        if (grounded && (Mathf.Abs(rb.linearVelocity.x) > 0.1f || Mathf.Abs(rb.linearVelocity.z) > 0.1f))
        {
            float shakeAmount = sprinting ? runShakeAmount : walkShakeAmount;
            Vector3 targetPosition = initialCamPosition + new Vector3(
                Mathf.Sin(Time.time * shakeFrequency) * shakeAmount,
                Mathf.Sin(Time.time * shakeFrequency * 0.5f) * shakeAmount,
                0);

            playerCam.localPosition = Vector3.Lerp(playerCam.localPosition, targetPosition, Time.deltaTime * 5f);
        }
        else
        {
            playerCam.localPosition = Vector3.Lerp(playerCam.localPosition, initialCamPosition, Time.deltaTime * 5f);
        }

        // Add slight tilt when strafing for immersion
        float tiltAmount = x * 30f;
        Quaternion targetTilt = Quaternion.Euler(0, 0, -tiltAmount);
        playerCam.localRotation = Quaternion.Slerp(playerCam.localRotation,
            Quaternion.Euler(xRotation, yRotation, 0) * targetTilt, Time.deltaTime * 3f);
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
    }

    private void PlayStepSound()
    {
        bool isMoving = Mathf.Abs(x) > 0.1f || Mathf.Abs(y) > 0.1f;

        if (grounded && isMoving)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.loop = true;
                audioSource.clip = stepSound;
                audioSource.pitch = sprinting ? 1.2f : 0.5f;
                audioSource.volume = isCrouching ? 0.3f : 1f;
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    private void UpdateUI()
    {
        healthText.text = health.ToString();
        imehealth.GetComponent<RectTransform>().sizeDelta = new Vector2(health, imehealth.GetComponent<RectTransform>().sizeDelta.y);
        staminaBar.fillAmount = stamina / maxStamina;
    }

    public void TogglePause()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isPaused = !canvasPause.gameObject.activeSelf;

            canvasPause.gameObject.SetActive(isPaused);
            canvasMain.gameObject.SetActive(!isPaused);

            Time.timeScale = isPaused ? 0 : 1;

            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isPaused;
        }
    }

    public void Die()
    {
        if (health <= 0)
        {
            rb.isKinematic = true;
            GetComponent<Collider>().enabled = false;
            this.enabled = false;

            canvasMati.gameObject.SetActive(true);

            Time.timeScale = 0;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void senter()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Senternya = !Senternya;
            Senter.gameObject.SetActive(Senternya);
        }
    }
}