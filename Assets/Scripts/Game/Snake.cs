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
    public Transform[] controlPoints = new Transform[3];
    // the reason there are only 3 control points is because the first one
    // always is the origin of the head (the object this script is attached to)

    [Header("Body MeshGen Settings")]
    [SerializeField] MeshSlice shape;
    public bool preview = true;
    [Range(0f,1f)] public float tPreview = 0;
    [Range(.05f, 1f)] public float thickness = .5f;
    // todo
    // modulate thickness with a bezier curve and map it to an array for matching values
    public Transform[] thicknessModulator = new Transform[4];
    public int segments = 5;
    public float segmentLength = 2f;
    [Range(2,16)] public int subDivs = 8;


// INTERNAL ATTRIBUTES
    Vector3 GetPos(int index) => controlPoints[index].position;
    private OrientedPoint[] positionsHistory = new OrientedPoint[1024];
    // keeps track of roughly 20 seconds of positions
    // note that this initializes all points at (0,0,0) [if constructor is Vector3]
    // so if no position is specified it will just unroll from the center
    private float[] thicknessMapping;
    private Mesh mesh;
    private int vc;
    private int step;
    private int stop;

    void Awake()
    {
        vc = shape.VertCount;
        if (shape.isSmooth) {
            step = 1;
            stop = 1;
        } else {
            step = 2;
            stop = 0;
        }

        if(shape.isSymmetrical) {
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
        thicknessMapping = new float[1024];
        DrawThicknessBezier();

        if(preview) {
            PopulateInitialPositions();
            DrawBodyPreview();
        } else {
            DrawBodyBezier();
        }
    }

    void DrawBodyPreview() {
        Gizmos.color = Color.white;
        int edgeRing = positionsHistory.Length / subDivs / 8;
        for (int i = 0; i < positionsHistory.Length; i+= edgeRing)
        {
            Gizmos.DrawSphere(positionsHistory[i].position, .02f);
            OrientedPoint origin = positionsHistory[i];
            float m = thicknessMapping[i];

            for (int v = 0; v < shape.baseVertices.Length - 1; v++)
            {
                Vector3 a = origin.GetDisplacedPoint(shape.baseVertices[v].point*thickness*m);
                Vector3 b = origin.GetDisplacedPoint(shape.baseVertices[v+1].point*thickness*m);
                Gizmos.DrawLine(a,b);
            }
        }
    }

    void PopulateInitialPositions() {
        //Debug.Log(thicknessMapping.Length);
        for (int i = 0; i < positionsHistory.Length; i++)
        {
            float t = i / (positionsHistory.Length -1f);
            OrientedPoint localOrigin = GetBezierPoint(t);
            positionsHistory[i] = localOrigin;

        }

        for (int i = 0; i < thicknessMapping.Length; i++)
        {
            float t = i / (positionsHistory.Length -1f);
            float m = GetBezierThick(t).position.x;
            thicknessMapping[i] = m;
            
        }
    }

    OrientedPoint GetBezierPoint(float t)
    {

        // setting points and handles
        Vector3 p0 = transform.position;
        Vector3 p1 = GetPos(0);
        Vector3 p2 = GetPos(1);
        Vector3 p3 = GetPos(2);

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

    void DrawBodyBezier() {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, .05f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(GetPos(0), .03f);
        Gizmos.DrawSphere(GetPos(1), .03f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(GetPos(2), .03f);
        
        Handles.DrawBezier(
            transform.position,
            GetPos(2),
            GetPos(0),
            GetPos(1),
            Color.white,
            EditorGUIUtility.whiteTexture,
            1f);
    }

    // todo refactor here
        OrientedPoint GetBezierThick(float t)
    {

        // setting points and handles
        Vector3 p0 = thicknessModulator[0].position;
        Vector3 p1 = thicknessModulator[1].position;
        Vector3 p2 = thicknessModulator[2].position;
        Vector3 p3 = thicknessModulator[3].position;

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

    void DrawThicknessBezier(){
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(thicknessModulator[0].position, .05f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(thicknessModulator[1].position, .03f);
        Gizmos.DrawSphere(thicknessModulator[2].position, .03f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(thicknessModulator[3].position, .03f);
        
        Handles.DrawBezier(
            thicknessModulator[0].position,
            thicknessModulator[3].position,
            thicknessModulator[1].position,
            thicknessModulator[2].position,
            Color.green,
            EditorGUIUtility.whiteTexture,
            1f);
    }

}
