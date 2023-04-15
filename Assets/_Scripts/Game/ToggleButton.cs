using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animation))]
public class ToggleButton : MonoBehaviour
{
    private Animation anim;

    private void Start()
    {
        anim = GetComponent<Animation>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collision on the Button");
        anim.Play(); 
        if (other.CompareTag("DroppedObject"))
        {
            if (other.GetComponent<FruitController>() != null
            && other.GetComponent<FruitController>().IsDropped)
            {
                Debug.Log("Dropped the cherries");
            }
        }
    }
}
