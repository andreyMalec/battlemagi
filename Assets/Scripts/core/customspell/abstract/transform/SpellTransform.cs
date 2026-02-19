using UnityEngine;

public struct SpellTransform {
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Forward;

    public SpellTransform(Transform transform) {
        Position = transform.position;
        Rotation = transform.rotation;
        Forward = transform.forward;
    }
}