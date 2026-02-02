using UnityEngine;

public class SpellView : MonoBehaviour {
    public bool IsAlive { get; private set; } = true;

    public void Kill() {
        IsAlive = false;
        Destroy(gameObject);
    }
}