using System.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Tail : MonoBehaviour
{
    public int tailSegmentsCount;
    public LineRenderer lineRenderer;
    public Vector3[] segmentsPositions;
    public Transform head;
    public float segmentsInterval = .7f;
    public float damping = .08f;
    public float trail = 200;

    private Vector3[] segmentsVelocities;

    void Start()
    {
        lineRenderer.positionCount = tailSegmentsCount;
        segmentsPositions = new Vector3[tailSegmentsCount];
        segmentsVelocities = new Vector3[tailSegmentsCount];
    }

    void Update()
    {
        segmentsPositions[0] = head.position; // assigns first position to head's origin
        for (int i = 1; i < segmentsPositions.Length; i++) // start at [1] since [0] is done
        {
            // variables for readability purposes only, to eliminate in optimization
            Vector3 target = segmentsPositions[i - 1];
            Vector3 current = segmentsPositions[i];

            // fixed value to maintain equal distance between all nodes
            Vector3 bufferDist = -head.forward * segmentsInterval;

            segmentsPositions[i] = Vector3.SmoothDamp(
                current,
                target + bufferDist,
                ref segmentsVelocities[i],
                damping + i / trail);
        }

        lineRenderer.SetPositions(segmentsPositions);
    }
}
