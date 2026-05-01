using UnityEngine;

public class LinearMotion : MonoBehaviour {
    public float speed = 5f;
    public float period = 5f;

    [SerializeField] private Stats stats;

    void FixedUpdate() {
        PeriodicallyMoveLeftToRight();
    }

    private void PeriodicallyMoveLeftToRight() {
        float t = Time.time % period;
        float halfPeriod = period / 2f;
        float direction = t < halfPeriod ? 1f : -1f;
        var s = speed * stats?.GetFinal(StatType.MoveSpeed) ?? 1f;
        transform.Translate(Vector3.right * (direction * Time.fixedDeltaTime * s));
    }
}