using UnityEngine;

public class SwayAndBob : MonoBehaviour
{
    [Header("References")]
    public Rigidbody playerRb;
    public Transform cam;
    public Player player; // Reference to the Player component

    [Header("Sway Settings")]
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.05f;
    public float swaySmooth = 16f; // Increased for smoother sway

    [Header("Bob Settings")]
    public float bobAmount = 0.05f;
    public float bobSpeed = 6f;
    public float runMultiplier = 1.5f;
    public float bobSmooth = 16f; // Added for smoother bob

    [Header("Jump Bump")]
    public float jumpBobStrength = 0.1f;
    public float bumpDamp = 8f; // Increased for smoother bump

    private Vector3 initPos;
    private Quaternion initRot;
    private Vector3 swayOffset;
    private Vector3 bobOffset;
    private float bobTimer;
    private float verticalBump;
    private bool wasGrounded;

    void Start()
    {
        initPos = transform.localPosition;
        initRot = transform.localRotation;

        // Get the Player component if not assigned
        if (player == null)
        {
            player = GetComponent<Player>();
            if (player == null)
            {
                Debug.LogError("Player component not found on " + gameObject.name);
            }
        }
    }

    void Update()
    {
        ApplySway();
        ApplyBob();
    }

    void ApplySway()
    {
        float mouseX = -Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = -Input.GetAxis("Mouse Y") * swayAmount;

        mouseX = Mathf.Clamp(mouseX, -maxSwayAmount, maxSwayAmount);
        mouseY = Mathf.Clamp(mouseY, -maxSwayAmount, maxSwayAmount);

        Vector3 targetSway = new Vector3(mouseX, mouseY, 0);
        swayOffset = Vector3.Lerp(swayOffset, targetSway, Time.deltaTime * swaySmooth);
    }

    void ApplyBob()
    {
        Vector3 flatVelocity = new Vector3(playerRb.linearVelocity.x, 0, playerRb.linearVelocity.z);
        float speed = flatVelocity.magnitude;
        bool grounded = IsGrounded();

        Vector3 targetBob = Vector3.zero;
        if (speed > 0.1f && grounded)
        {
            bobTimer += Time.deltaTime * bobSpeed * (Input.GetKey(KeyCode.LeftShift) ? runMultiplier : 1f);
            float bobX = Mathf.Cos(bobTimer) * bobAmount;
            float bobY = Mathf.Abs(Mathf.Sin(bobTimer)) * bobAmount;
            targetBob = new Vector3(bobX, bobY, 0);
        }

        bobOffset = Vector3.Lerp(bobOffset, targetBob, Time.deltaTime * bobSmooth);

        if (!wasGrounded && grounded)
        {
            verticalBump = -jumpBobStrength;
        }
        verticalBump = Mathf.Lerp(verticalBump, 0, Time.deltaTime * bumpDamp);

        transform.localPosition = Vector3.Lerp(transform.localPosition, initPos + swayOffset + bobOffset + new Vector3(0, verticalBump, 0), Time.deltaTime * 16f);
        wasGrounded = grounded;
    }

    bool IsGrounded()
    {
        if (player == null) return false;
        return player.grounded;
    }
}
