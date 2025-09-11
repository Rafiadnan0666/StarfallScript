using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroy : MonoBehaviour
{
    

    // Update is called once per frame
    void Start()
    {
        Destroy(this.gameObject,0.5f * Time.deltaTime);   
    }
}
