using UnityEngine;

public class LocalEntityManager : IEntityManager {
    public GameObject Spawn(OwnerId ownerId, GameObject prefab, Vector3 pos, Quaternion rot) {
        return Object.Instantiate(prefab, pos, rot);
    }

    public void Despawn(GameObject gameObject) {
        if (gameObject == null) return;
        Object.Destroy(gameObject);
    }
}