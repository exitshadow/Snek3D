using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class RoadSegment : MonoBehaviour
{
    [Header("Mesh Generation Settings")]
    [SerializeField] MeshSlice extrusionShape;
    [Range(0, 1)] public float tGizmos = 0; // interpolation point at preview
    [Range(.05f, 5f)] public float scale = 1;
    [Range(2, 64)] public int subDivs = 16;

    [Header("Bézier Curves settings")]
    public Transform[] controlPoints = new Transform[4];
    Vector3 GetPos(int index) => controlPoints[index].position;

    // internal attributes
    private Mesh procSeg;
    private int vc;
    private int step;
    private int stop;

    private void Awake()
    {
        vc = extrusionShape.VertCount;

        if (extrusionShape.isSmooth) {
            step = 1;
            stop = 1;
        } 
        else {
            step = 2;
            stop = 0;
        }

        // TODO set symmetries for X or Y axis
        if (extrusionShape.isSymmetrical) {
            for (int i = 0; i < vc; i++)
            {
                // assign mapping coordinate to the y axis and bringing it to uv space
                extrusionShape.baseVertices[i].c = (extrusionShape.baseVertices[i].point.y + 1) / 2;
            }
        } // else rely on mesh data

        procSeg = new Mesh();
        procSeg.name = "Procedural Segment";
        GetComponent<MeshFilter>().sharedMesh = procSeg;
    }

    private void Start()
    {
        //GenerateMesh();
    }

    private void Update()
    {
        GenerateMesh();
    }


    private void GenerateMesh() {
        procSeg.Clear();

        List<Vector3> inVertices = new List<Vector3>();
        List<Vector3> inNormals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        // populate vertices
        for (int slice = 0; slice < subDivs; slice++)
        {
            float t = slice / (subDivs - 1f);
            OrientedPoint localOrigin = GetBezierPoint(t);

            for (int i = 0; i < extrusionShape.VertCount; i++)
            {
                inVertices.Add(localOrigin.GetDisplacedPoint(extrusionShape.baseVertices[i].point*scale));
                
                if (extrusionShape.isSmooth) {
                    // if the shape is smooth the orientation of the normal is the same as the point
                    inNormals.Add(localOrigin.GetOrientationPoint(extrusionShape.baseVertices[i].point));
                } else {
                    // otherwise rely on data input
                    inNormals.Add(localOrigin.GetOrientationPoint(extrusionShape.baseVertices[i].normal));
                }

                uvs.Add(new Vector2(t*8, extrusionShape.baseVertices[i].c));
            }
        }

        // read vertices to draw triangles
        // loop in slices
        for (int s = 0; s < subDivs - 1; s++)
        {
            // vc for vertex count
            int root = s * vc ;
            int rootNext = (s + 1) * vc;

            //Debug.Log(root);
            //Debug.Log(rootNext);

            // loop in mesh vertices
            // this will not work correctly with split vertices for hard edges
            // TODO v += 2 for hard edges
            for (int v = 0; v < vc - stop ; v+= step)
            {
                int node_a = extrusionShape.edgeLinksNodes[v];
                int node_b = extrusionShape.edgeLinksNodes[v+1];

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

        procSeg.SetVertices(inVertices);
        procSeg.SetNormals(inNormals);
        procSeg.SetUVs(0, uvs);
        procSeg.SetTriangles(triangles, 0);
    }


    private void OnDrawGizmos()
    {

        for (int i = 0; i < controlPoints.Length; i++)
        {
            Gizmos.DrawCube(GetPos(i), new Vector3(.05f, .05f, .05f));
        }

        Handles.DrawBezier(
            GetPos(0),
            GetPos(3),
            GetPos(1),
            GetPos(2),
            Color.white,
            EditorGUIUtility.whiteTexture,
            1f);

        OrientedPoint testPoint = GetBezierPoint(tGizmos);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(testPoint.position, .03f);
        //Handles.PositionHandle(testPoint.position, testPoint.rotation);

        void DrawVert(OrientedPoint vert, Vector3 disp, float scale = 1)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(vert.GetDisplacedPoint(disp * scale), .03f);
            Gizmos.color = Color.white;
        }

        for (int i = 0; i < extrusionShape.baseVertices.Length; i++)
        {
            DrawVert(testPoint, extrusionShape.baseVertices[i].point, scale);
        }

        Vector3[] verts = extrusionShape.baseVertices.Select(v => testPoint.GetDisplacedPoint(v.point * scale)).ToArray();

        for (int i = 0; i < extrusionShape.edgeLinksNodes.Length - 1; i++)
        {
            Vector3 a = verts[extrusionShape.edgeLinksNodes[i]];
            Vector3 b = verts[extrusionShape.edgeLinksNodes[i + 1]];
            Gizmos.DrawLine(a, b);
        }
    }

    OrientedPoint GetBezierPoint(float t)
    {

        // setting points and handles
        Vector3 p0 = GetPos(0);
        Vector3 p1 = GetPos(1);
        Vector3 p2 = GetPos(2);
        Vector3 p3 = GetPos(3);

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

}
