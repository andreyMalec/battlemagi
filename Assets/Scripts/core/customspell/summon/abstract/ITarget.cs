using UnityEngine;

public interface ITarget {
    public Vector3 Position { get; }
    public bool IsPlayer { get; }
    public bool IsSpell { get; }
    public OwnerId OwnerId { get; }
    public GameObject Get { get; }
}