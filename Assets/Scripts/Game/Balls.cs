using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Balls : MonoBehaviour
{
    private Rigidbody rb;

    void Start() {
        rb = transform.GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        Vector3 dir = collision.GetContact(0).normal;

        if (collision.transform.CompareTag("Player")) {
        rb.AddForce(dir, ForceMode.Impulse);
        } else rb.AddForce(dir * .5f, ForceMode.Impulse);
        
    }

}
