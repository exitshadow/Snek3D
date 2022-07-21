using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeOperator : MonoBehaviour
{
    // TODO
    // optimize positions history (array not list)
    // set an initialized position to start (eg with a curve)
    // => see HeadOperator.cs

    public float movingSpeed = 10;
    public float steeringSpeed = 200;

    public int gap = 10;
    public int snekInitialSize = 5;
    public GameObject snekBody;
    public GameObject snekTail;

    private List<GameObject> bodyParts = new List<GameObject>();
    private List<Vector3> positionsHistory = new List<Vector3>();

    private Vector3[] dummyArray = new Vector3[12];

    void Start()
    {
        for (int i = 0; i < snekInitialSize; i++)
        {
            GrowSnake();
        }

        // InvokeRepeating("GrowSnake", 1f, 1f);

        for (int i = 0; i < dummyArray.Length; i++)
        {
            Debug.Log(dummyArray[i]);
        }

    }
    void Update()
    {
        // crusing moving forward
        transform.position += transform.forward * movingSpeed * Time.deltaTime;

        // move in the two dimensions
        float h_steerDirection = Input.GetAxis("Horizontal");
        float v_SteerDirection = Input.GetAxis("Vertical");
        transform.Rotate(Vector3.up * h_steerDirection * steeringSpeed * Time.deltaTime);
        transform.Rotate(Vector3.left * v_SteerDirection * steeringSpeed * Time.deltaTime);

        

    }

    void FixedUpdate() {
        // store head's position history
        positionsHistory.Insert(0, transform.position);
        if(positionsHistory.Count > 1024) {
            positionsHistory.RemoveAt(positionsHistory.Count-1);
        }
        Debug.Log(positionsHistory.Count);

        // make body parts follow using positions history
        int index = 0;
        foreach (GameObject body in bodyParts) {
            Vector3 point = positionsHistory[Mathf.Min(index * gap, positionsHistory.Count-1)];
            Vector3 followDirection = point - body.transform.position;
            body.transform.position += followDirection * movingSpeed * Time.deltaTime;
            body.transform.LookAt(point);
            index++;
        }

    }
    private void GrowSnake() {
        if (bodyParts.Count == 0) {
            GameObject body = Instantiate(snekTail);
            bodyParts.Add(body);
        } 
        else {
            GameObject body = Instantiate(snekBody);
            bodyParts.Insert(0,body);
        }
    }
}
