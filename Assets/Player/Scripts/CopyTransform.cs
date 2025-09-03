using UnityEngine;

public class CopyTransform : MonoBehaviour {
    public enum Direction {
        FromTargetToSelf,
        FromSelfToTarget
    }

    public Transform target;
    public Direction direction = Direction.FromTargetToSelf;

    [Header("Position")] public bool x;
    public bool y;
    public bool z;

    [Header("Rotation")] public bool rotationX;
    public bool rotationY;
    public bool rotationZ;

    public void Update() {
        var receiver = transform;
        if (direction == Direction.FromSelfToTarget)
            receiver = target;
        var origin = target;
        if (direction == Direction.FromSelfToTarget)
            origin = transform;

        if (rotationX && rotationY && rotationZ)
            receiver.rotation = origin.rotation;
        else {
            float xx;
            float yy;
            float zz;

            if (rotationX)
                xx = origin.eulerAngles.x;
            else
                xx = receiver.eulerAngles.x;
            if (rotationY)
                yy = origin.eulerAngles.y;
            else
                yy = receiver.eulerAngles.y;
            if (rotationZ)
                zz = origin.eulerAngles.z;
            else
                zz = receiver.eulerAngles.z;

            receiver.eulerAngles = new Vector3(xx, yy, zz);
        }

        if (x && y && z)
            receiver.position = origin.position;
        else {
            float xx;
            float yy;
            float zz;

            if (x)
                xx = origin.position.x;
            else
                xx = receiver.position.x;
            if (y)
                yy = origin.position.y;
            else
                yy = receiver.position.y;
            if (z)
                zz = origin.position.z;
            else
                zz = receiver.position.z;

            receiver.position = new Vector3(xx, yy, zz);
        }
    }
}