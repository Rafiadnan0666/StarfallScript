using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Tele : MonoBehaviour
{
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private Bosses1 bos1;
    [SerializeField] private Bosses2 bos2;
    [SerializeField] private Bosses3 bos3;

    [SerializeField] private string Next;

    private bool isTriggered = false;

    void Start()
    {

        isTriggered = false;

        if (bos1 != null)
        {
           
            bos1.enabled = false;
        }
        else if (bos2 != null)
        {
        
            bos2.enabled = false;
        }
        else if (bos3 != null)
        {
          
            bos3.enabled = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("Player") && !isTriggered)
        {
            if (spawnPoint != null) {
                Destroy(spawnPoint, 1f);
            }else
            {
                SceneManager.LoadScene(Next);
            }
            
            
            isTriggered = true;

            if (bos1 != null)
            {

                bos1.enabled = true;
            }
            else if (bos2 != null)
            {

                bos2.enabled = true;
            }
            else if (bos3 != null)
            {

                bos3.enabled = true;
            }
        }
    }
}