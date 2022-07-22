using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingFruit : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 3f;

    void Update()
    {
        transform.Rotate(new Vector3(0, 1f, 0)* rotationSpeed * Time.deltaTime, Space.World);
    }
}
