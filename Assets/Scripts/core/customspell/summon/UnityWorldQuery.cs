using System.Collections.Generic;
using System.Linq;
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
        var targets = new List<(GameObject, ITarget)>();
        var casters = new List<SpellCaster>(SpellCaster.Active);
        foreach (var caster in casters) {
            if (caster == null) continue;
            if (caster.gameObject == null) continue;
            if ((caster.transform.position - pos).sqrMagnitude <= r * r)
                targets.Add((caster.gameObject, caster));
        }

        var spells = new List<SpellInstance>(SpellInstance.Active);
        foreach (var spellInstance in spells) {
            if (spellInstance == null) continue;
            if (spellInstance.gameObject == null) continue;
            if (!spellInstance.Bind.Context.View.IsAlive) continue;
            if ((spellInstance.transform.position - pos).sqrMagnitude <= r * r)
                targets.Add((spellInstance.gameObject, spellInstance));
        }

        targets.Sort((x, y) => (x.Item2.Position - pos).sqrMagnitude.CompareTo((y.Item2.Position - pos).sqrMagnitude));
        return RemoveDuplicatedGameObject(targets);
    }

    private List<ITarget> RemoveDuplicatedGameObject(List<(GameObject, ITarget)> list) {
        return list.GroupBy(x => x.Item1).Select(g => g.First().Item2).ToList();
    }
}