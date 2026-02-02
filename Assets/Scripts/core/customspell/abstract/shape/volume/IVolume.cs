using UnityEngine;

public interface IVolume {
    Vector3 Center { get; }
    bool Contains(Vector3 point);
}