using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class BezierUtils
{
    public static OrientedPoint CalculateBezierPoint(float t, Transform[] controls, bool global = true)
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

        return new OrientedPoint(pos, rot, new Vector3(0,0,0));
    }

    public static void DrawBezierCurve(Transform[] controls)
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
