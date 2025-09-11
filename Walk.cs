//using Photon.Realtime;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class Walk : MonoBehaviour
//{
//    [Header("IK Settings")]
//    public LayerMask groundLayer;
//    public float stepDistance = 1.5f; // Distance between each step
//    public float stepHeight = 0.2f;   // Maximum height of the step arc
//    public float footMoveSpeed = 5f; // Speed of foot movement

//    [Header("Feet Settings")]
//    public Transform leftFoot;
//    public Transform rightFoot;
//    public Transform body;
//    public float footOffset = 0.1f;  // Offset above the ground

//    private Vector3 leftFootTarget;
//    private Vector3 rightFootTarget;
//    private bool isLeftFootMoving;

//    void Start()
//    {
//        // Initialize foot targets
//        leftFootTarget = leftFoot.position;
//        rightFootTarget = rightFoot.position;
//    }

//    void Update()
//    {
//        // Procedural movement for each foot
//        HandleFootStep(ref leftFootTarget, leftFoot, isLeftFootMoving);
//        HandleFootStep(ref rightFootTarget, rightFoot, !isLeftFootMoving);

//        // Alternate foot movement
//        if (Vector3.Distance(leftFoot.position, leftFootTarget) < 0.1f)
//        {
//            isLeftFootMoving = false;
//        }
//        if (Vector3.Distance(rightFoot.position, rightFootTarget) < 0.1f)
//        {
//            isLeftFootMoving = true;
//        }

//        // Adjust body position to keep it centered between the feet
//        AdjustBodyPosition();
//    }

//    void HandleFootStep(ref Vector3 target, Transform foot, bool canMove)
//    {
//        if (!canMove) return;

//        Vector3 forwardStep = body.forward * stepDistance;
//        Vector3 newTarget = foot.position + forwardStep;

//        // Check for ground
//        if (Physics.Raycast(newTarget + Vector3.up, Vector3.down, out RaycastHit hit, 2f, groundLayer))
//        {
//            target = hit.point + Vector3.up * footOffset;

//            // Add arc for stepping motion
//            target.y += Mathf.Sin(Time.time * footMoveSpeed) * stepHeight;
//        }

//        // Move foot to target
//        foot.position = Vector3.Lerp(foot.position, target, Time.deltaTime * footMoveSpeed);
//    }

//    void AdjustBodyPosition()
//    {
//        // Find the midpoint between the two IK foot positions
//        Vector3 middlePoint = (ik_foot_l.position + ik_foot_r.position) / 2;

//        // Perform a raycast from the middle point down to detect the ground
//        if (Physics.Raycast(middlePoint + Vector3.up, Vector3.down, out RaycastHit hit, 2f, groundLayer))
//        {
//            // Adjust root bone height based on the ground level
//            Vector3 targetPosition = hit.point + Vector3.up * footOffset;
//            root.position = Vector3.Lerp(root.position, targetPosition, Time.deltaTime * 5f);
//        }
//        else
//        {
//            // If no ground is detected, keep the root at its current position
//            root.position = Vector3.Lerp(root.position, middlePoint, Time.deltaTime * 5f);
//        }
//    }
//}

