using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPlayer : MonoBehaviour
{
    public GameObject leftHandTarget;
    public GameObject rightHandTarget;
    public GameObject leftHand;
    public GameObject rightHand;
    [SerializeField]
    public float animationSpeed = 5.0f;
    public float rotationSpeed = 2.0f;

    private float x;
    private float y;

    void Update()
    {
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");

        //if ((Mathf.Abs(x) > 0.1f || Mathf.Abs(y) > 0.1f))
        //{
           
        //        leftHandTarget.transform.position = Vector3.Lerp(leftHandTarget.transform.position, leftHand.transform.position, animationSpeed * Time.deltaTime);
        //        rightHandTarget.transform.position = Vector3.Lerp(rightHandTarget.transform.position, rightHand.transform.position, animationSpeed * Time.deltaTime);
           
        //}
    }
}



