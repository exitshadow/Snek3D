using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class BodyOperator : MonoBehaviour
{
    [Tooltip("Choose the head the body should follow")] public GameObject head;

    [Header("Mesh Generation Settings")]
    [SerializeField] MeshSlice bodyShape;
    [Range(.05f, 1f)] public float bodyThickness = .5f;
    [Range(2, 16)] public int intialDetail = 8;
}

// TODO
// adapt code from RoadSegment.cs to the body
//
//  [in SnakeOperator / HeadOperator]
//  =>  this is no longer a bézier curve
//      therefore we need to
//          - parse the history of positions instead of a curve
//          - populate the history of positions before starting
//              - (so maybe define a nice nested curve like the
//                 spiralling sleeping snake that will populate
//                 positions at the start. see drawing)
//
//  [in BodyOperator]
//  =>  it is no longer static
//      so we'll need to
//          - fetch & update the head's history of positions
//          - move the mesh following that array
//              - recalculating the whole mesh seems expensive
//                  => find other way to anchor & deform the mesh
//
//  =>  it needs to grow
//          - design procedure to add geometry on the go
//
//  =>  a snake isn't a tube
//          - adapt MeshGen code to include a closing tip for the tail
//          - create function to modulate the thickness by length