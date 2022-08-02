using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float movingSpeed = 3f;
    [SerializeField] private float steeringSpeed = 200f;
    [SerializeField] private Transform body;
    [SerializeField] private Transform _grabOrigin;

    [HideInInspector] public Transform GrabOrigin {
        get { return _grabOrigin; }

    }

    private Rigidbody rb;
    private Vector3 screenPos;
    float h_steerDirection;
    float v_SteerDirection;

    private Transform grabTarget;

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

        if (other.CompareTag("Grabbable")) {
            grabTarget = other.transform;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {

        transform.position += transform.forward * movingSpeed * Time.deltaTime;

        h_steerDirection = Input.GetAxis("Horizontal");
        v_SteerDirection = Input.GetAxis("Vertical");

        screenPos = mainCamera.WorldToScreenPoint(transform.position);

        transform.Rotate(Vector3.up * h_steerDirection * steeringSpeed * Time.deltaTime);
        transform.Rotate(Vector3.left * v_SteerDirection * steeringSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("pressed space");

            if (grabTarget != null) 
            {
                Debug.Log("Plz release");

                if (grabTarget.GetComponent<FruitController>() != null)
                {
                    Debug.Log("FruitController exists.");
                    grabTarget.GetComponent<FruitController>().Release();
                }
            }
        }
    }
}
