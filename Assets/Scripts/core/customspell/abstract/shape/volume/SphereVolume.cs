using UnityEngine;

public struct SphereVolume : IVolume {
    public Vector3 Center { get; set; }
    public float Radius;

    public bool Contains(Vector3 point)
        => Vector3.Distance(point, Center) <= Radius;
}