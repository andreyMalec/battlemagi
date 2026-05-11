using System.Collections;
using UnityEngine;

public class SpellView : MonoBehaviour {
    public float beforeEndThreshold = 1f;
    public bool IsAlive { get; private set; } = true;
    public bool scaleShape = false;
    public Stats Stats { get; private set; }

    private void Awake() {
        Stats = gameObject.AddComponent<Stats>();
        gameObject.AddComponent<Statusable>();
    }

    private Coroutine _waitAndKillCoroutine;

    public void WaitAndKill(float waitTime, ISpellContext context) {
        if (_waitAndKillCoroutine != null)
            StopCoroutine(_waitAndKillCoroutine);
        _waitAndKillCoroutine = StartCoroutine(WaitAndKillCoroutine(waitTime, context));
    }

    private IEnumerator WaitAndKillCoroutine(float waitTime, ISpellContext context) {
        yield return new WaitForSeconds(waitTime);
        Kill(context);
    }

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