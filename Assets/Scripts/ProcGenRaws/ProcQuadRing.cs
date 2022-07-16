using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ProcQuadRing : MonoBehaviour
{
    public enum UVProjection {
        Polar,
        TopDown
    }

    [Range(.01f, 1)] public float radius = .5f;
    [Range(.01f,1)] public float thickness = .2f;
    [Range(3,32)] public int subDivs = 16;
    public UVProjection uvProjection = UVProjection.Polar;

    private float radiusOuter => radius + thickness; // shortcut notation for get {}
    private int vertexCount => subDivs * 2;
    Mesh quadRing;
    
    void OnDrawGizmosSelected()
    {
        ProcUtils.DrawWireCircle(transform.position, transform.rotation, radius, subDivs);
        ProcUtils.DrawWireCircle(transform.position, transform.rotation, radiusOuter, subDivs);
    }

    private void Awake()
    {
        quadRing = new Mesh();
        quadRing.name = "Quad Ring";
        GetComponent<MeshFilter>().sharedMesh = quadRing;
        
    }

    private void Update() {
        GenerateQuadRing(GetUvProjection());
    }

    private UVProjection GetUvProjection()
    {
        return uvProjection;
    }

    void GenerateQuadRing(UVProjection uvProjection)
    {
        quadRing.Clear(); // clears before updating every time

        // int vCount = vertexCount;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangleIndices = new List<int>();

        // populate our vertices list with the right coordinates
        for (int i = 0; i < subDivs + 1; i++)
        // note there is 1 more vertex to
        // allow the verts to split and the UVs to map correctly
        {
            float t = i / (float)subDivs;
            float angRadians = t * Mathf.PI *2;

            // direction in which we generate the vertices pairs for the quad ring
            Vector2 dir = ProcUtils.GetUnitVectorByAngle(angRadians);

            vertices.Add(dir * radiusOuter);    // add outer ring vert first
            vertices.Add(dir * radius);         // add inner ring vert after
            normals.Add(Vector3.forward);       // add normals twice
            normals.Add(Vector3.forward);       // so it matches the vertices

            switch (uvProjection)
            {   
                case UVProjection.Polar:
                    // polar uvs method
                    uvs.Add( new Vector2(t, 1));
                    uvs.Add( new Vector2(t, 0));
                    break;
                case UVProjection.TopDown:
                    uvs.Add(dir * .5f + Vector2.one * .5f);
                    uvs.Add(dir * (radius / radiusOuter) * .5f + Vector2.one * .5f);
                break;
                    
            }

        }

        // draw triangles with the vertices indices
        for (int i = 0; i < subDivs; i++)
        {
            int indexRoot = i * 2; // helper to locate our root index for drawing
            int indexInnerRoot = indexRoot + 1;
            int indexOuterNext = (indexRoot + 2);
            int indexInnerNext = (indexRoot + 3);

            // draw external triangle of the quad
            triangleIndices.Add(indexRoot);
            triangleIndices.Add(indexOuterNext);
            triangleIndices.Add(indexInnerNext);

            // draw inner triangle of the quad
            triangleIndices.Add(indexRoot);
            triangleIndices.Add(indexInnerNext);
            triangleIndices.Add(indexInnerRoot);

        }

        quadRing.SetVertices(vertices);
        quadRing.SetTriangles(triangleIndices, 0);
        quadRing.SetNormals(normals);
        quadRing.SetUVs(0, uvs);
    }
}
