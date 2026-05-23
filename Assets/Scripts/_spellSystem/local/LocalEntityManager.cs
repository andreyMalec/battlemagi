using UnityEngine;

public class LocalEntityManager : IEntityManager {

    public void Despawn(GameObject gameObject) {
        if (gameObject == null) return;
        Object.Destroy(gameObject);
    }
}