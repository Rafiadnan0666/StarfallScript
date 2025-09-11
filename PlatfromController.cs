using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    // Parent GameObject that holds the platforms
    public GameObject parentPlatform;

    // Start is called before the first frame update
    void Start()
    {
        // Check if the parent is assigned
        if (parentPlatform != null)
        {
            // Loop through all the children of the parent and assign movement
            foreach (Transform child in parentPlatform.transform)
            {
                // Ensure each child has its own PlatformMovement script
                PlatformMovement platformMovement = child.gameObject.AddComponent<PlatformMovement>();
                platformMovement.Initialize(child);
            }
        }
        else
        {
            Debug.LogWarning("Parent platform is not assigned.");
        }
    }
}

public class PlatformMovement : MonoBehaviour
{
    // Enum to define direction of movement
    public enum Direction
    {
        Up,
        Down
    }

    public Direction platformDirection = Direction.Up; // Initial direction
    public float moveSpeed = 0.5f; // Speed of the platform
    public float moveRangeUp = 1f; // How high the platform goes
    public float moveRangeDown = 1f; // How low the platform goes
    private Vector3 startPosition;
    private Vector3 targetPosition;

    // Timer for random direction change
    private float directionChangeTimer = 0f;
    public float directionChangeInterval = 3f; // How often the direction changes

    // Initialization with unique start position for each platform
    public void Initialize(Transform platformTransform)
    {
        startPosition = platformTransform.position; // Save the starting position of the platform
        SetTargetPosition();
    }


    void Update()
    {
        MovePlatform();

        // Timer for changing direction randomly
        directionChangeTimer += Time.deltaTime;
        if (directionChangeTimer >= directionChangeInterval)
        {
            ChangeDirectionRandomly();
            directionChangeTimer = 0f;
        }
    }

    void MovePlatform()
    {
        // Move the platform towards the target position
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // If it reaches the target, switch direction
        if (transform.position == targetPosition)
        {
            SetTargetPosition();
        }
    }

    void SetTargetPosition()
    {
        // Calculate the target position based on direction
        if (platformDirection == Direction.Up)
        {
            targetPosition = startPosition + Vector3.up * moveRangeUp; // Move up
        }
        else
        {
            targetPosition = startPosition - Vector3.up * moveRangeDown; // Move down
        }
    }

    void ChangeDirectionRandomly()
    {
        // Randomly choose a new direction (up or down)
        platformDirection = (Random.Range(0, 2) == 0) ? Direction.Up : Direction.Down;
    }
}
