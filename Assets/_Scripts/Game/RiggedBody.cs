using System;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// * UP AND RUNNING FOR FIRST ROUND OF GENERATION
// TODO refine capsule colliders


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class RiggedBody : MonoBehaviour
{
    [Header("Mesh Generation")]
    [SerializeField] private MeshSlice shape;
    [SerializeField] private float thickness = .5f;
    [SerializeField] private Transform[] thicknessCurve = new Transform[4];
    [SerializeField] private int initialSegmentsCount = 10;
    [SerializeField] private int maxSegmentsCount = 20;
    [SerializeField] private float segmentsInterval = .5f;
    [SerializeField] private SkinnedMeshRenderer skin;
    [SerializeField] private bool canSewToHead;
    private Mesh headMesh;


    [Space]
    [Header("Rigging Animation")]
    [SerializeField] Transform headTransform;
    [SerializeField] private float movementDamping = .08f;
    [SerializeField] private float trailResponse = 200f;
    [SerializeField] private GameObject riggingPrefab;
    private Vector3[] velocities;
    

    [Space]
    [Header("Debugging")]    
    [SerializeField] private bool debug = true;
    [SerializeField] private LineRenderer linePreview;

    // mesh gen internal data
    private Mesh mesh;
    private int currentSegmentsCount;
    private float[] thicknessMapping;
    private BoneWeight[] weights;

    // rigging gen internal data
    private Transform[] bones;
    private Matrix4x4[] bindPoses;

    // rigging dynamics
    private Rigidbody[] rigidbodies;
    private CharacterJoint[] characterJoints;
    private CapsuleCollider[] capsuleColliders;

    private void Awake()
    {
        headMesh = headTransform.GetComponent<SkinnedMeshRenderer>().sharedMesh;
        transform.position = headTransform.position;

        InitializeArmature();
        GenerateArmature();

        InitializeMesh();
        GenerateMesh();
    }

    private void OnDrawGizmos()
    {
        //if (debug) DrawBodyPreview();
    }

    private void Update()
    {
        Move();
    }

    private void InitializeMesh()
    {
        mesh = new Mesh();
        mesh.name = "Snake Body";
        mesh.bindposes = bindPoses;
        skin.sharedMesh = mesh;
        skin.bones = bones;

        // movement setup
        velocities = new Vector3[maxSegmentsCount];

        thicknessMapping = new float[maxSegmentsCount];
        for (int i = 0; i < thicknessMapping.Length; i++)
        {
            float t = i / (currentSegmentsCount - 1f);
            float m = BezierUtils.CalculateBezierPoint(t, thicknessCurve).position.x;
            thicknessMapping[i] = m;
        }
    }

    private void InitializeArmature()
    {
        currentSegmentsCount = initialSegmentsCount;

        // rig setup
        rigidbodies = new Rigidbody[maxSegmentsCount];
        capsuleColliders = new CapsuleCollider[maxSegmentsCount];
        characterJoints = new CharacterJoint[maxSegmentsCount];

        bones = new Transform[maxSegmentsCount];
        bindPoses = new Matrix4x4[maxSegmentsCount];
    }

    private void GenerateArmature()
    {
        for (int i = 0; i < bones.Length; i++)
        {
            GameObject prefab = Instantiate(riggingPrefab as GameObject);
            bones[i] = prefab.transform;
            // would be good to refactor this...
            rigidbodies[i] = bones[i].gameObject.GetComponent<Rigidbody>();
            capsuleColliders[i] = bones[i].gameObject.GetComponent<CapsuleCollider>();

            if(bones[i].gameObject.GetComponent<CharacterJoint>() != null)
            {
                characterJoints[i] = bones[i].gameObject.GetComponent<CharacterJoint>();
                if(i==0) characterJoints[0].connectedBody = headTransform.GetComponent<Rigidbody>();
                else characterJoints[i].connectedBody = bones[i-1].GetComponent<Rigidbody>();
            }

            if(i == 0) {
                bones[0].parent = transform;
                bones[0].localPosition = Vector3.zero;
            } else {
                bones[i].parent = transform;
                //bones[i].parent = bones[i-1];
                bones[i].localPosition = new Vector3(0,0, -segmentsInterval * (i - 1));

            }

            //bones[i].localPosition = new Vector3(0, 0, -segmentsInterval * i);
            bones[i].localRotation = Quaternion.identity;
            
            bindPoses[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
            // this is actually also working just fine. problem is in the triangles somewhere
        }

        for (int i = currentSegmentsCount + 1; i < capsuleColliders.Length ; i++)
        {
            bones[i].gameObject.GetComponent<CapsuleCollider>().enabled = false;
        }

    }

    private void GenerateMesh()
    {
        // shape management
        int vc = shape.VertCount;
        int step, stop;

        if (shape.isSmooth) {
            step = 1;
            stop = 1;
        } else {
            step = 2;
            stop = 0;
        }

        if (shape.isSymmetrical)
        {
            for (int i = 0; i < vc; i++)
            {
                // mapping the Y height of the mesh to the V position on the uv
                shape.baseVertices[i].c = (shape.baseVertices[i].point.y + 1) / 2f;
            }
        }

        // starting generation
        Debug.Log("Generating body mesh...");
        mesh.Clear();

        // vertices buffer data
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        List<BoneWeight> boneWeights = new List<BoneWeight>();
        Vector3[] origins = new Vector3[currentSegmentsCount];

        Mesh headMeshBase = headMesh;

        #region populate vertices
        // looping through each slice
        for (int slice = 0; slice < currentSegmentsCount; slice++)
        {
            // mapping thickness curve to points in slice
            float t = slice / (currentSegmentsCount - 1f);
            float m = BezierUtils.CalculateBezierPoint(t, thicknessCurve).position.x;
            //thicknessMapping[slice] = m;

            // use the current bone as the origin for drawing a slice
            Transform origin = skin.bones[slice];


            // looping in the vertices around each slice for extrusion
            for (int i = 0; i < vc; i++)
            {
                // default case
                Vector3 point = shape.baseVertices[i].point;
                Vector3 normal = shape.baseVertices[i].normal;
                float v = shape.baseVertices[i].c;
                Vector3 pos = point * thickness * m; // relative position on the slice
                Vector3 vertex = origin.localPosition + origin.localRotation * pos;

                // if there is a head to sew into
                if (slice == 0 && canSewToHead)
                {
                    // ! the code here is scripted specifically for the mesh provided in the test!
                    // will have to be refactored with a new SO that provides the right indices

                    // the idea is that we replace the positions of the current vertex
                    // by the positions of index-n vertices of the corresponding vertex
                    // in the original head mesh of the snake

                    // beware that the mesh here has been modified so vertices do match,
                    // otherwise we should specificy for the entire full ring

                    // so we need to access the local position of the vertices in questions,
                    // then transform it in world coordinates
                    // then transform it in our local
                    // origin = headTransform;
                    Matrix4x4 headMeshToWorld = headTransform.localToWorldMatrix;
                    Matrix4x4 worldToLocal = origin.worldToLocalMatrix;

                    // ok the thing is it ain’t working because the calculations were wrong in the first place, this bit is likely to be correct
                    vertex = worldToLocal.MultiplyPoint3x4(headMeshToWorld.MultiplyPoint3x4(headMeshBase.vertices[i]));

                    if (i == 12) vertex = origin.localRotation * worldToLocal.MultiplyPoint3x4(headMeshToWorld.MultiplyPoint3x4(headMeshBase.vertices[0]));
                    // this is correct.

                    // BUT

                    // this is only working the first time lmfao
                    
                }

                // assign position
                vertices.Add(vertex);

                // assign normal
                if (shape.isSmooth) {
                    normals.Add(origin.rotation * point);
                } else normals.Add(origin.rotation * normal);
                    // the normal of a smooth point is the same as its unique vertex
                    // while hard edges do have split vertices

                // assign uv
                uvs.Add(new Vector2(t * currentSegmentsCount / 2, v));

                bindPoses[slice] = origin.worldToLocalMatrix * transform.localToWorldMatrix; // this was already done actually
                
                // assign weights
                BoneWeight weight = new BoneWeight();
                weight.boneIndex0 = slice;
                weight.weight0 = 1;
                boneWeights.Add(weight);
                mesh.bindposes = bindPoses;
                mesh.RecalculateNormals();
            }
        }
        #endregion

        #region draw triangles
        // adding triangle indices
        // loop in slices
        for (int s = 0; s < currentSegmentsCount - 1; s++)
        {
            int root = s * vc ;
            int rootNext = (s + 1) * vc;

            //Debug.Log(root);
            //Debug.Log(rootNext);

            // loop in mesh vertices
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

                mesh.RecalculateNormals();
            }
        }

        #endregion

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);

        weights = new BoneWeight[currentSegmentsCount * vc];
            for (int i = 0; i < boneWeights.Count; i++) weights[i] = boneWeights[i];
        
        mesh.boneWeights = weights;
        skin.sharedMesh = mesh;
        //skin.bones = bones;

    }

    /// <summary>
    /// Moves all bones along with a SmoothDamp
    /// </summary>
    private void Move()
    {
        skin.bones[0].position = headTransform.position;
        skin.bones[0].rotation = headTransform.rotation;

        for (int i = 1; i < maxSegmentsCount; i++)
        {
            Vector3 target = skin.bones[i-1].position;

            Vector3 interval = Vector3.forward * segmentsInterval * -1f;
            Vector3 current = skin.bones[i].position;
            
            Vector3 dir = target - current;

            if (i == 0) skin.bones[i].position = headTransform.position;
            else
            {
                skin.bones[i].position = Vector3.SmoothDamp(
                    current,
                    target + interval,
                    ref velocities[i],
                    movementDamping + i / trailResponse);
            }

            
            skin.bones[i].rotation = Quaternion.LookRotation(dir);
        }
    }

    public void GrowSnake() {
        if (currentSegmentsCount < maxSegmentsCount)
        {
            Debug.Log("Grow the snake");
            linePreview.positionCount++; // * testing only
            currentSegmentsCount++;

            // reassign bidposes
            for (int i = 0; i < bones.Length; i++)
            {
                //bindPoses[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
            }

            bones[currentSegmentsCount].gameObject.GetComponent<CapsuleCollider>().enabled = true;

            GenerateMesh();
        }
        else Debug.Log("Snake has attained its maximum length.");
        
    }

    /// <summary>
    /// Draws the axis from the bones but still can't figure how to draw the circles round ha
    /// </summary>
    void DrawBodyPreview()
    {
        for (int i = 0; i < maxSegmentsCount; i++)
        {
            float t = i / (currentSegmentsCount - 1f);
            float m = BezierUtils.CalculateBezierPoint(t, thicknessCurve).position.x;

            Transform origin = skin.bones[i];
            // Debug.Log(skin.bones.Length);
            // Debug.Log(i);

            Gizmos.color = Color.black;
            Gizmos.DrawSphere(origin.position, .02f);

            Gizmos.color = Color.yellow;
            if (i < currentSegmentsCount - 1) {
                Gizmos.DrawLine(bones[i].position, bones[i+1].position);
            }

            Gizmos.color = Color.green;
            Vector3 yAxis = origin.TransformDirection(Vector3.up) * .5f;
            Gizmos.DrawRay(origin.position, yAxis);

            Gizmos.color = Color.blue;
            Vector3 zAxis = origin.TransformDirection(Vector3.forward) * .5f;
            Gizmos.DrawRay(origin.position, zAxis);

            Gizmos.color = Color.red;
            Vector3 xAxis = origin.TransformDirection(Vector3.right) * .5f;
            Gizmos.DrawRay(origin.position, xAxis);

            Gizmos.color = Color.white;
            for (int v = 0; v < shape.VertCount - 1 ; v++)
            {
                Vector3 pointA = shape.baseVertices[v].point;
                Vector3 posA = pointA * thickness * m; // relative position on the slice
                Vector3 a = origin.position + origin.rotation * posA; // mapped to current origin
                
                Vector3 pointB = shape.baseVertices[v + 1].point;
                Vector3 posB = pointA * thickness * m; // relative position on the slice
                Vector3 b = origin.position + origin.rotation * posB; // mapped to current origin

                Gizmos.DrawLine(a,b);
            }
        }
    }


}
