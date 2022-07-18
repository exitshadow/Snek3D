using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>
    /// Controls the mesh generation and the positions of Snake's tail
    /// </summary>

// todo
// [x] use Tail.cs positions management functions to translate it
//      to the points that will generate or update a new mesh
//  * use the positions to generate bones and skin them to the slice's
//      vertices (by adapting the existing GenerateMesh() function)
//  * split controls from Snake.cs to SnakeController.cs
//      that will only manage the player's controls on the snake and
//      leave the body logic to this script.

// TODO = Dynamically growing the snake
//  might need a List<OrientedPoint> and List<float>...
//  in fact it's more complicated than that because they cannot be modified
//  by reference, might need to find a trick with a wider array and swap values
//  on the go depending on the size (ie a variable like currentLength)
//  ... seems feasible it just gives the snake a maximum length

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SnakeBody : MonoBehaviour
{
    [Header("Mesh Generation")]
    [SerializeField] MeshSlice shape;
    [SerializeField] bool preview = true;
    [SerializeField] [Range(.5f, 1f)] float bodyThickness = .5f;
    [SerializeField] Transform[] thicknessModulator = new Transform[4];
    [SerializeField] int initialSegmentsCount = 10;
    [SerializeField] int maxSegmentsCount = 50;
    [SerializeField] float segmentsInterval = .5f;
    [SerializeField] [Range(1, 3)] int subDivs = 1; // * might not be needed anymore


    [Header("Initial Pose")] // * might not be needed anymore
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
    private float[] thicknessMapping; // could add that to OrientedPoint :thinking:
    private Mesh mesh;
    private int vc;
    private ushort step;
    private ushort stop;
    private int currentSegmentsCount;

    void Awake()
    {
        vc = shape.VertCount;
        currentSegmentsCount = initialSegmentsCount;

        if (shape.isSmooth)
        {
            step = 1;
            stop = 1;
        }
        else
        {
            step = 2;
            stop = 0;
        }

        if (shape.isSymmetrical)
        {
            for (int i = 0; i < vc; i++)
            {
                shape.baseVertices[i].c = (shape.baseVertices[i].point.y + 1) / 2f;
            }
        }

        mesh = new Mesh();
        mesh.name = "Snake Body";
        GetComponent<MeshFilter>().sharedMesh = mesh;

    }

    void Start()
    {
        // * testing purposing only
        linePreview.positionCount = initialSegmentsCount;

        segmentPoints = new OrientedPoint[maxSegmentsCount];
    }

    void Update()
    {
        // this is equivalent to the old PopulateInitialPositions()
        segmentPoints[0].position = head.position;
        linePreview.SetPosition(0, segmentPoints[0].position); //*testing only
        for (int i = 1; i < currentSegmentsCount; i++)
        {
            Vector3 target = segmentPoints[i-1].position;
            Vector3 current = segmentPoints[i].position;
            Vector3 bufferDist = -head.forward * segmentsInterval;

            segmentPoints[i].position = Vector3.SmoothDamp(
                current,
                target + bufferDist,
                ref segmentPoints[i].velocity,
                movementDamping + i / trailResponse);
            
            // * testing only
            linePreview.SetPosition(i, segmentPoints[i].position);
        } 
    }

    private void GenerateBodyMesh() {
        Debug.Log("Starting mesh generation");
    }

    public void GrowSnake() {
        if (currentSegmentsCount < maxSegmentsCount)
        {
            Debug.Log("Grow the snake");
            linePreview.positionCount++; // * testing only
            currentSegmentsCount++;
            GenerateBodyMesh();
        }
        else Debug.Log("Snake has attained its maximum length.");
        
    }
}
