using System.Collections.Generic;
using UnityEngine;

public class UnityWorldQuery : IWorldQuery {
    private readonly int _enemyMask = 1 << LayerMask.NameToLayer("Player");

    public ITarget FindClosestEnemy(Vector3 pos, float radius) {
        var colliders = Physics.OverlapSphere(pos, radius, _enemyMask);

        ITarget closest = null;
        float best = float.MaxValue;

        foreach (var c in colliders) {
            var target = c.GetComponent<ITarget>();
            if (target == null) continue;

            float d = (target.Position - pos).sqrMagnitude;
            if (d < best) {
                best = d;
                closest = target;
            }
        }

        return closest;
    }

    public bool HasLineOfSight(Vector3 from, Vector3 to) {
        return !Physics.Linecast(from, to, _enemyMask);
    }

    public IEnumerable<ITarget> FindEnemiesInRadius(Vector3 pos, float r) {
        foreach (var c in Physics.OverlapSphere(pos, r, _enemyMask))
            yield return c.GetComponent<ITarget>();
    }
}