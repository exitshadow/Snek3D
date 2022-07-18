using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>
    /// Controls the mesh generation and the positions of Snake's tail
    /// </summary>

// todo
//  => use Tail.cs positions management functions to translate it
//      to the points that will generate or update a new mesh
//  => use the positions to generate bones and skin them to the slice's
//      vertices (by adapting the existing GenerateMesh() function)
//  => split controls from Snake.cs to SnakeController.cs that will only
//      manage the player's controls on the snake and leave the body logic
//      to this script.

[RequireComponent(typeof(MeshSlice))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SnakeBody : MonoBehaviour
{
    [Header("Mesh Generation")]
    [SerializeField] MeshSlice shape;
    [SerializeField] bool preview = true;
    [SerializeField] [Range(.5f, 1f)] float bodyThickness = .5f;
    [SerializeField] Transform[] thicknessModulator = new Transform[4];
    [SerializeField] int initialSegmentsCount = 20;
    [SerializeField] float segmentsInterval = .5f;
    [SerializeField] [Range(1, 3)] int subDivs = 1;

    [Header("Initial Pose")]
    [SerializeField] Transform[] initialPoseControlPoints = new Transform[4];

    [Header("Movement")]
    [SerializeField] Transform head;
    [SerializeField] float movementDamping = .08f;
    [SerializeField] float trailResponse = 200f;

    // * testing purposes only
    [SerializeField] LineRenderer linePreview;

    private OrientedPoint[] segmentPoints;
    // NOTE OrientedPoint struct contains
    //      position, rotation and velocity


    void Start()
    {
        segmentPoints = new OrientedPoint[initialSegmentsCount];
    }

    void Update()
    {
        segmentPoints[0].position = head.position;
        for (int i = 1; i < segmentPoints.Length; i++)
        {
            Vector3 target = segmentPoints[i-1].position;
            Vector3 current = segmentPoints[i].position;
            Vector3 bufferDist = -head.forward * segmentsInterval;

            segmentPoints[i].position = Vector3.SmoothDamp(
                current,
                target + bufferDist,
                ref segmentPoints[i].velocity,
                movementDamping + i / trailResponse);
            
            linePreview.SetPosition(i, segmentPoints[i].position);
        } 
    }
}
