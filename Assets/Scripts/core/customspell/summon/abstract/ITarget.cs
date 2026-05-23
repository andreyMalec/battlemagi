using JetBrains.Annotations;
using UnityEngine;

public interface ITarget {
    public Vector3 Position { get; }
    public bool IsPlayer { get; }
    public bool IsSpell { get; }
    public ParticipantId OwnerId { get; }
    public ulong ObjectId { get; }
    public bool CanGet { get; }
    public GameObject Get { get; }
}