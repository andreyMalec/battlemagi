using UnityEngine;

public class LocalEntityManager : IEntityManager {
    public GameObject Spawn(OwnerId ownerId, GameObject prefab, Vector3 pos, Quaternion rot) {
        return Object.Instantiate(prefab, pos, rot);
    }

    public OwnerId Owner(GameObject obj) {
        return new OwnerId(0);
    }

    public bool IsOwner(GameObject obj) {
        return true;
    }
}