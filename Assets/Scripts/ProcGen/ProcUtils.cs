using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ProcUtils
{
    public static Vector2 GetUnitVectorByAngle(float angRadians)
    {
        return new Vector2(
            Mathf.Cos(angRadians),
            Mathf.Sin(angRadians)
        );
    }
    
    public static void DrawWireCircle(
        Vector3 pos,
        Quaternion rot,
        float radius,
        int subDivs = 32)
        {
        Vector3[] points3D = new Vector3[subDivs];

        // populating our array with coordinates
        for (int i = 0; i < subDivs; i++)
        {
            float t = i / (float)subDivs; // fraction factor on total number of intervals
            float angRadians = t * Mathf.PI * 2; // PI*2 (TAU) is a full revolution in radians

            Vector2 point2D = GetUnitVectorByAngle(angRadians);

            // displace coordinates & applies rotation & scale
            // NOTE : a Quaternion can be multiplied with a Vec2 and will return a Vec3, i guess.
            points3D[i] = pos + rot * point2D * radius;

        }

        // drawing with the data in the array
        for (int i = 0; i < subDivs-1; i++)
        {
            // very mini spheres
            Gizmos.DrawSphere(points3D[i], .02f);
            // and lines between them
            Gizmos.DrawLine(points3D[i], points3D[i+1]);
        }
        // draw line between the last point to 0
        Gizmos.DrawLine(points3D[subDivs-1], points3D[0]);

    }

}
