using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoCollider : MonoBehaviour
{
    private Collider colide;
    void Start()
    {
        colide = GetComponent<Collider>();
    }

    void Update()
    {
        if (colide ==  null)
        {
            Debug.Log("ga da");
        }
        else
        {
            colide.enabled = false;
        }
            
    }
}
