using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class MeshSlice : ScriptableObject
{
    [System.Serializable]
    public class Vertex {
        public Vector2 point;
        public Vector2 normal;
        // if the shading is smooth vertex values are identical to point values
        public float c; // local coordinate (only 1 axis on the UV);
        
        // and any other vertex data one might need
    }
    [Tooltip("If your mesh has hard edges you MUST provide two vertices per position")]
    public bool isSmooth = true;
    public bool isSymmetrical = true;
    public Vertex[] baseVertices;

    // a int format is needed for later referring to it as the indices of the triangles
    [Tooltip(
        "Define vertex indices that the edges should link together. Think of it as indices steps")]
    public int[] edgeLinksNodes;

    public int VertCount => baseVertices.Length;
    public int EdgeCount => baseVertices.Length;
    public Vertex[] verts => baseVertices;

}
