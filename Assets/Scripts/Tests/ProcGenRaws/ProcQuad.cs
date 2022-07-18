// using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProcQuad : MonoBehaviour
{
    private void Awake() {
        Mesh quad = new Mesh();
        quad.name = "Procedural Quad";

        List<Vector3> points = new List<Vector3>() {
            new Vector3(-1,-1),
            new Vector3( -1, 1),
            new Vector3( 1, 1),
            new Vector3( 1,-1)
        };
        
        List<Vector3> normals = new List<Vector3> {
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
            Vector3.forward
        };

        List<Vector2> uvs = new List<Vector2> {
            new Vector2(1,0),
            new Vector2(1,1),
            new Vector2(0,1),
            new Vector2(0,0)
        };

        int[] triIndices = new int[] {
            1, 0, 2,
            0, 3, 2
        };

        quad.SetVertices(points);
        quad.SetNormals(normals);
        quad.SetUVs(0, uvs);
        quad.triangles = triIndices;

        GetComponent<MeshFilter>().sharedMesh = quad;
    }
}
