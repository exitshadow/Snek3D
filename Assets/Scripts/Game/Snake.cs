using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(MeshFilter))]
public class Snake : MonoBehaviour
{
    // FIELDS IN THE INSPECTOR
    [Header("Controls Settings")]
    public float speed = 5;
    public float sensitivity = 200;

    [Header("Initial Position Settings")]
    // for now, let's keep it with a single curve
    // for later a more complex implementation
    // https://catlikecoding.com/unity/tutorials/curves-and-splines/
    public Transform[] controlPoints = new Transform[4];
    // it can refer to itself for the first handle point

    [Header("Body MeshGen Settings")]
    [SerializeField] MeshSlice shape;
    public bool preview = true;
    [Range(0f, 1f)] public float tPreview = 0;
    [Range(.05f, 1f)] public float thickness = .5f;
    public Transform[] thicknessModulator = new Transform[4];
    public int segments = 5;
    public float segmentLength = 2f;
    [Range(2, 16)] public int subDivs = 8;


    // INTERNAL ATTRIBUTES
    Vector3 GetPos(int index) => controlPoints[index].position;
    private OrientedPoint[] positionsHistory = new OrientedPoint[1024];
    // keeps track of roughly 20 seconds of positions
    // note that this initializes all points at (0,0,0) [if constructor is Vector3]
    // so if no position is specified it will just unroll from the center
    private float[] thicknessMapping = new float[1024];
    private Mesh mesh;
    private int vc;
    private int step;
    private int stop;

    void Awake()
    {
        vc = shape.VertCount;
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
        mesh.name = "Procedural Body";
        GetComponent<MeshFilter>().sharedMesh = mesh;

    }

    void OnDrawGizmos()
    {
        //thicknessMapping = new float[1024];
        DrawBezierCurve(thicknessModulator);

        if (preview)
        {
            PopulateInitialPositions();
            DrawBodyPreview();
        }
        else
        {
            DrawBezierCurve(controlPoints);
        }
    }

    void Update()
    {
        PopulateInitialPositions(false);
        GenerateBodyMesh();
    }

    void DrawBodyPreview()
    {
        Gizmos.color = Color.white;
        int edgeRing = positionsHistory.Length / subDivs / 8;
        for (int i = 0; i < positionsHistory.Length; i += edgeRing)
        {
            Gizmos.DrawSphere(positionsHistory[i].position, .02f);
            OrientedPoint origin = positionsHistory[i];
            float m = thicknessMapping[i];

            for (int v = 0; v < shape.baseVertices.Length - 1; v++)
            {
                Vector3 a = origin.GetDisplacedPoint(shape.baseVertices[v].point * thickness * m);
                Vector3 b = origin.GetDisplacedPoint(shape.baseVertices[v + 1].point * thickness * m);
                Gizmos.DrawLine(a, b);
            }
        }

        // start point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(controlPoints[0].position, .05f);
        // control points
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(controlPoints[1].position, .03f);
        Gizmos.DrawSphere(controlPoints[2].position, .03f);
        // end point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(controlPoints[3].position, .03f);
        Gizmos.color = Color.white;
    }

    void PopulateInitialPositions(bool global=true)
    {
        //Debug.Log("Populating initial positions");
        for (int i = 0; i < positionsHistory.Length; i++)
        {
            OrientedPoint localOrigin;
            float t = i / (positionsHistory.Length - 1f);

            if(global) localOrigin = CalculateBezierPoint(t, controlPoints);
            else localOrigin = CalculateBezierPoint(t, controlPoints, false);

            positionsHistory[i] = localOrigin;
            //Debug.Log($"position at index {i} : {positionsHistory[i].position}");

        }


        for (int i = 0; i < thicknessMapping.Length; i++)
        {
            float t = i / (positionsHistory.Length - 1f);
            float m = CalculateBezierPoint(t, thicknessModulator).position.x;
            thicknessMapping[i] = m;

        }
    }

    void GenerateBodyMesh()
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
        int edgeRing = positionsHistory.Length / subDivs / 8;

        for (int slice = 0; slice < positionsHistory.Length; slice+= edgeRing)
        {
            OrientedPoint localOrigin = positionsHistory[slice];
            //Debug.Log($"position at slice {slice}: {localOrigin.position}");

            float m = thicknessMapping[slice];
            //Debug.Log($"thickness modulator = {m}");

            for (int i = 0; i < shape.VertCount; i++)
            {
                inVertices.Add(localOrigin.GetDisplacedPoint(shape.baseVertices[i].point*thickness*m));
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
        for (int s = 0; s < edgeRing - 1; s++)
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

    OrientedPoint CalculateBezierPoint(float t, Transform[] controls, bool global = true)
    {
        Vector3 p0, p1, p2, p3;

        if(global) {
            // setting points and handles
            p0 = controls[0].position;
            p1 = controls[1].position;
            p2 = controls[2].position;
            p3 = controls[3].position;
        } else {
            p0 = controls[0].InverseTransformPoint(controls[0].position);
            p1 = controls[0].InverseTransformPoint(controls[1].position);
            p2 = controls[0].InverseTransformPoint(controls[2].position);
            p3 = controls[0].InverseTransformPoint(controls[3].position);
        }

        // first layer of interpolation
        Vector3 a = Vector3.Lerp(p0, p1, t);
        Vector3 b = Vector3.Lerp(p1, p2, t);
        Vector3 c = Vector3.Lerp(p2, p3, t);

        // second layer of interpolation
        Vector3 d = Vector3.Lerp(a, b, t);
        Vector3 e = Vector3.Lerp(b, c, t);

        // last layer of interpolation
        Vector3 pos = Vector3.Lerp(d, e, t);

        // catching tangent line to store its normalized direction
        Vector3 dir = (e - d).normalized;

        // and pointing the rotation in the same forwards as dir
        Quaternion rot = Quaternion.LookRotation(dir);
        // note: this isn't very exact but it does the job

        return new OrientedPoint(pos, rot);
    }

    void DrawBezierCurve(Transform[] controls)
    {
        // start point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(controls[0].position, .05f);
        // control points
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(controls[1].position, .03f);
        Gizmos.DrawSphere(controls[2].position, .03f);
        // end point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(controls[3].position, .03f);
        Gizmos.color = Color.white;

        Handles.DrawBezier(
            controls[0].position,
            controls[3].position,
            controls[1].position,
            controls[2].position,
            Color.white,
            EditorGUIUtility.whiteTexture,
            1f);
    }

}
