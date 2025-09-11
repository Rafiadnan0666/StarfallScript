using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cheker : MonoBehaviour
{
   
        public Fall fall;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                Debug.Log("Ground Checker Activated!");

                if (fall != null)
                {
                    fall.isLanded = true;
                    fall.LandPod();
                }
                else
                {
                    Debug.LogError("Fall script reference is missing!");
                }
            }
        }
    
}
