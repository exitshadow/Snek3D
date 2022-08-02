using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] float movingSpeed = 3f;
    [SerializeField] float steeringSpeed = 200f;
    [SerializeField] Transform body;
    [SerializeField] Transform rotationAxis;

    private Rigidbody rb;
    private Vector3 screenPos;
    float h_steerDirection;
    float v_SteerDirection;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Enter!");
        if (other.CompareTag("GrowObject"))
        {
            if (body.GetComponent<RiggedBody>() != null)
            {
                body.GetComponent<RiggedBody>().GrowSnake();
            }
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        rotationAxis.position = transform.position;
        rotationAxis.rotation = mainCamera.transform.rotation;

        transform.position += transform.forward * movingSpeed * Time.deltaTime;

        h_steerDirection = Input.GetAxis("Horizontal");
        v_SteerDirection = Input.GetAxis("Vertical");

        screenPos = mainCamera.WorldToScreenPoint(transform.position);

        transform.Rotate(Vector3.up * h_steerDirection * steeringSpeed * Time.deltaTime);
        transform.Rotate(Vector3.left * v_SteerDirection * steeringSpeed * Time.deltaTime);
    }
}
