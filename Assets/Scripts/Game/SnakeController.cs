using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    [SerializeField] float movingSpeed = 3f;
    [SerializeField] float steeringSpeed = 200f;
    [SerializeField] Transform body;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collision Enter!");
        if (other.CompareTag("GrowObject"))
        {
            body.GetComponent<SnakeBody>().GrowSnake();
        }
    }

    void Update()
    {
        transform.position += transform.forward * movingSpeed * Time.deltaTime;

        float h_steerDirection = Input.GetAxis("Horizontal");
        float v_SteerDirection = Input.GetAxis("Vertical");
        transform.Rotate(Vector3.up * h_steerDirection * steeringSpeed * Time.deltaTime);
        transform.Rotate(Vector3.left * v_SteerDirection * steeringSpeed * Time.deltaTime);
    }
}
