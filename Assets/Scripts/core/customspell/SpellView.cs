using System.Collections;
using UnityEngine;

public class SpellView : MonoBehaviour {
    public float beforeEndThreshold = 1f;
    public bool IsAlive { get; private set; } = true;

    public Vector3 Position => transform.position;

    public void Kill(ISpellContext context) {
        if (!IsAlive) return;
        IsAlive = false;

        context.Event.OnKill(this);

        StartCoroutine(WaitForParticlesToDie());
    }

    private IEnumerator WaitForParticlesToDie() {
        var particleSystems = GetComponentsInChildren<ParticleSystem>();
        bool anyAlive;
        do {
            anyAlive = false;
            foreach (var ps in particleSystems) {
                if (ps.IsAlive(true)) {
                    anyAlive = true;
                    break;
                }
            }

            yield return null;
        } while (anyAlive);

        DI.Get<IEntityManager>().Despawn(transform.parent.gameObject);
    }
}