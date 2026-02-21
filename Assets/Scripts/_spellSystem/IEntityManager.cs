using UnityEngine;

public interface IEntityManager {

    GameObject Spawn(
        OwnerId ownerId,
        GameObject prefab,
        Vector3 pos,
        Quaternion rot
    );

    void Destroy(GameObject gameObject);
}