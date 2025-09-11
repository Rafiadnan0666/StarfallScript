using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfall
{
    public class CameraLook : MonoBehaviour
    {
        public float sensitivity = 2f; 
        private Vector3 lastMousePosition;
        private bool isSliding;

        void Update()
        {
            if (Input.GetMouseButtonDown(1)) 
            {
                isSliding = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(1)) 
            {
                isSliding = false;
            }

            if (isSliding)
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;
                transform.position -= new Vector3(delta.x, delta.y, 0) * sensitivity * Time.deltaTime;
                lastMousePosition = Input.mousePosition;
            }
        }
    }
}
