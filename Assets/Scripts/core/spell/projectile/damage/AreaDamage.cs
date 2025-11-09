using System.Collections.Generic;
using UnityEngine;

public class AreaDamage : ISpellDamage {
    private readonly SpellData data;
    private readonly BaseSpell spell;

    public AreaDamage(BaseSpell s, SpellData d) {
        spell = s;
        data = d;
    }

    public bool OnEnter(Collider other) {
        // track clients we've already damaged or should skip using a HashSet for fast lookup
        var excludedSet = new HashSet<ulong>();
        ulong[] excludedArray = System.Array.Empty<ulong>();
        bool excludedChanged = false;

        if (!data.canSelfDamage) {
            excludedSet.Add(spell.OwnerClientId);
            excludedChanged = true;
        }

        // apply damage to the initial collider and add its client id to excluded
        var firstClient = DamageUtils.TryApplyDamage(spell, data, other);
        if (firstClient != ulong.MaxValue) {
            if (excludedSet.Add(firstClient)) excludedChanged = true;
        }

        if (excludedChanged) excludedArray = new List<ulong>(excludedSet).ToArray();

        Collider[] buffer = new Collider[32];
        int found;
        while (true) {
            found = Physics.OverlapSphereNonAlloc(spell.transform.position, data.areaRadius, buffer, ~0,
                QueryTriggerInteraction.Collide);
            if (found < buffer.Length) break;
            // buffer was too small, grow and retry
            buffer = new Collider[buffer.Length * 2];
        }

        for (int i = 0; i < found; i++) {
            var hit = buffer[i];
            // pass the current excluded list so TryApplyDamage will skip those clients
            var damagedClient = excludedArray.Length == 0
                ? DamageUtils.TryApplyDamage(spell, data, hit, null, true)
                : DamageUtils.TryApplyDamage(spell, data, hit, excludedArray, true);
            // if TryApplyDamage reports a newly damaged client, add it to excluded
            if (damagedClient != ulong.MaxValue && excludedSet.Add(damagedClient)) {
                excludedArray = new List<ulong>(excludedSet).ToArray();
            }
        }

        return true;
    }

    public bool OnExit(Collider other) {
        return false;
    }

    public bool Update() {
        return false;
    }
}