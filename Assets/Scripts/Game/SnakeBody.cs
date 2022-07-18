using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
    [SerializeField] bool debug = true;
    [SerializeField] [Range(.01f, 1f)] float bodyThickness = .5f;
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

        if(debug) linePreview.positionCount = initialSegmentsCount;

        segmentPoints = new OrientedPoint[maxSegmentsCount];
        thicknessMapping = new float[maxSegmentsCount];

        PopulateInitialPositions(false);

        if(debug) {
            for (int i = 0; i < currentSegmentsCount; i++)
            {
                linePreview.SetPosition(i, segmentPoints[i].position);
            }
        }

        GenerateBodyMesh();

    }

    void OnDrawGizmos()
    {
        BezierUtils.DrawBezierCurve(thicknessModulator);
        BezierUtils.DrawBezierCurve(initialPoseControlPoints);
        if (preview) DrawBodyPreview();
        
    }

    void Update()
    {
        // this is equivalent to the old PopulateInitialPositions()
        segmentPoints[0].position = head.position;
        segmentPoints[0].rotation = Quaternion.LookRotation(head.position);

        if(debug) linePreview.SetPosition(0, segmentPoints[0].position); //*testing only

        for (int i = 1; i < currentSegmentsCount; i++)
        {
            Vector3 target = segmentPoints[i-1].position;
            Vector3 current = segmentPoints[i].position;
            Vector3 bufferDist = -head.forward * segmentsInterval;
            Vector3 dir = target - current;

            segmentPoints[i].position = Vector3.SmoothDamp(
                current,
                target + bufferDist,
                ref segmentPoints[i].velocity,
                movementDamping + i / trailResponse);
            
            segmentPoints[i].rotation = Quaternion.LookRotation(dir);

            if(debug) linePreview.SetPosition(i, segmentPoints[i].position);

        }

        GenerateBodyMesh();
    }


    public void GrowSnake() {
        if (currentSegmentsCount < maxSegmentsCount)
        {
            Debug.Log("Grow the snake");
            linePreview.positionCount++; // * testing only
            currentSegmentsCount++;
            // GenerateBodyMesh();
        }
        else Debug.Log("Snake has attained its maximum length.");
        
    }
    private void GenerateBodyMesh()
    {
        //Debug.Log("Generating body mesh");

        mesh.Clear();
        //Debug.Log("Mesh Cleared");

        List<Vector3> inVertices = new List<Vector3>();
        List<Vector3> inNormals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        //Debug.Log("Lists of vertex information created");

        // populate vertices

        for (int slice = 0; slice < currentSegmentsCount; slice++)
        {
            OrientedPoint localOrigin = segmentPoints[slice];
            //Debug.Log($"position at slice {slice}: {localOrigin.position}");

            float t = slice / (currentSegmentsCount - 1f);
            float m = BezierUtils.CalculateBezierPoint(t, thicknessModulator).position.x;
            thicknessMapping[slice] = m;
            //Debug.Log($"thickness modulator = {m}");

            for (int i = 0; i < shape.VertCount; i++)
            {
                inVertices.Add(
                    head.InverseTransformPoint(
                        localOrigin.GetDisplacedPoint(
                            shape.baseVertices[i].point*bodyThickness*m)));
                //Debug.Log("added vertices");

                if (shape.isSmooth) {
                    // if the shape is smooth the orientation of the normal is the same as the point
                    inNormals.Add(localOrigin.GetOrientationPoint(shape.baseVertices[i].point));
                } else {
                    // otherwise rely on data input
                    inNormals.Add(localOrigin.GetOrientationPoint(shape.baseVertices[i].normal));
                }
                //Debug.Log("added normals");

                // todo probably some tweaking for this one
                uvs.Add(new Vector2(slice / 8, shape.baseVertices[i].c));
                
            }
        }

        // read vertices to draw triangles
        // loop in slices
        for (int s = 0; s < currentSegmentsCount - 1; s++)
        {
            // vc for vertex count
            int root = s * vc ;
            int rootNext = (s + 1) * vc;

            //Debug.Log(root);
            //Debug.Log(rootNext);

            // loop in mesh vertices
            // this will not work correctly with split vertices for hard edges
            for (int v = 0; v < vc - stop ; v+= step)
            {
                int node_a = shape.edgeLinksNodes[v];
                int node_b = shape.edgeLinksNodes[v+1];

                int a = root + node_a;
                int b = root + node_b;
                int ap = rootNext + node_a;
                int bp = rootNext + node_b;

                triangles.Add(a);
                triangles.Add(ap);
                triangles.Add(b);
                //Debug.Log($"Face{a}, Tri01: {a}, {ap}, {b}");

                triangles.Add(b);
                triangles.Add(ap);
                triangles.Add(bp);
                //Debug.Log($"Face{b}, Tri02: {b}, {ap}, {bp}");
            }
        }

        mesh.SetVertices(inVertices);
        mesh.SetNormals(inNormals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
    }

    private void PopulateInitialPositions(bool global=true)
    {
        //Debug.Log("Populating initial positions");
        for (int i = 0; i < currentSegmentsCount; i++)
        {
            OrientedPoint localOrigin;
            float t = i / (currentSegmentsCount - 1f);

            if(global) localOrigin = BezierUtils.CalculateBezierPoint(t, initialPoseControlPoints);
            else localOrigin = BezierUtils.CalculateBezierPoint(t, initialPoseControlPoints, false);

            segmentPoints[i] = localOrigin;
            //Debug.Log($"position at index {i} : {positionsHistory[i].position}");

        }

        for (int i = 0; i < currentSegmentsCount; i++)
        {
            float t = i / (currentSegmentsCount - 1f);
            float m = BezierUtils.CalculateBezierPoint(t, thicknessModulator).position.x;
            thicknessMapping[i] = m;

        }
    }

    private void DrawBodyPreview()
    {
        //Debug.Log("Draw body preview");

        Gizmos.color = Color.white;
        for (int i = 0; i < currentSegmentsCount; i ++)
        {
            Gizmos.DrawSphere(segmentPoints[i].position, .02f);
            OrientedPoint origin = segmentPoints[i];
            float m = thicknessMapping[i];

            for (int v = 0; v < shape.baseVertices.Length - 1; v++)
            {
                Vector3 a = origin.GetDisplacedPoint(shape.baseVertices[v].point * bodyThickness * m);
                Vector3 b = origin.GetDisplacedPoint(shape.baseVertices[v + 1].point * bodyThickness * m);
                Gizmos.DrawLine(a, b);
            }
        }
    }

}
