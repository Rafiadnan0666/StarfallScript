using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollActivate : MonoBehaviour
{
    public bool touched;
    void SetKinematic(bool newValue)
    {
        //Get an array of components that are of type Rigidbody
        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();

        
        foreach (Rigidbody rb in bodies)
        {
            rb.isKinematic = newValue;
        }
    }
    // Use this for initialization
    void Start()
    {
        touched = false;
        SetKinematic(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (touched == true)
        {
            SetKinematic(false);
            GetComponent<Animator>().enabled = false;
            GetComponent<BoxCollider>().enabled = false;
        }

        /*
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
        }
        */
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet") || collision.gameObject.CompareTag("Player"))
        {
            touched = true;
        }
    }
   

}
