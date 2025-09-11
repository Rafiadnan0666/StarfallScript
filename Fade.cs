using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    public Image Image;
    void Start()
    {
        Image image = GetComponent<Image>();
        image.CrossFadeAlpha(0,100,false);
    }

    
}
