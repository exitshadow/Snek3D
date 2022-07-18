using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateToTarget : MonoBehaviour
{
    public Transform target;
    public float rotationSpeed = 20f;
    public float maxDelta = 5f;

    Vector3 direction;

    void Update()
    {
        // rotation
        direction = target.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // following
        transform.position = Vector3.MoveTowards(transform.position, target.position, maxDelta * Time.deltaTime);

    }
}
