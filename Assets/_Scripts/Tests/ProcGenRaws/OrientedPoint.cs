using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct OrientedPoint
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;

    // constructor with full Quaternion
    public OrientedPoint(Vector3 position, Quaternion rotation, Vector3 velocity) {
        this.position = position;
        this.rotation = rotation;
        this.velocity = velocity;
    }

    // forward looking Quaternion constructor overload
    public OrientedPoint(Vector3 position, Vector3 forward, Vector3 velocity) {
        this.position = position;
        this.rotation = Quaternion.LookRotation(forward);
        this.velocity = velocity;
    }

    // returns a point from our local point but displaced by a vector
    public Vector3 GetDisplacedPoint(Vector3 displacement) {
        return position + rotation * displacement;
    }

    // same as before but returns only the orientation of the vector
    public Vector3 GetOrientationPoint(Vector3 displacement) {
        return rotation * displacement;
    }
}
