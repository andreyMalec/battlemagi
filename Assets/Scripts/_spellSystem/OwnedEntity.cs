using UnityEngine;

public readonly struct OwnedEntity {
    public readonly OwnerId Owner;
    public readonly GameObject Object;

    public OwnedEntity(OwnerId owner, GameObject obj) {
        Owner = owner;
        Object = obj;
    }
}