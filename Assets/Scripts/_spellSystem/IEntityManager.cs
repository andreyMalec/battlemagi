using UnityEngine;

public interface IEntityManager {
    GameObject Spawn(
        OwnerId ownerId,
        GameObject prefab,
        Vector3 pos,
        Quaternion rot
    );

    OwnerId Owner(GameObject obj);

    bool IsOwner(GameObject obj);
}