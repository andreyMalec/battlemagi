using UnityEngine;

public class LinearMotion : MonoBehaviour, ITarget {
    public float speed = 5f;
    public float period = 5f;

    void FixedUpdate() {
        PeriodicallyMoveLeftToRight();
    }

    private void PeriodicallyMoveLeftToRight() {
        float t = Time.time % period;
        float halfPeriod = period / 2f;
        float direction = t < halfPeriod ? 1f : -1f;
        transform.Translate(Vector3.right * (direction * speed * Time.fixedDeltaTime));
    }

    public Vector3 Position => transform.position;
}