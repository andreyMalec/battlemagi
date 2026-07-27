using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellView : MonoBehaviour {
    public float beforeEndThreshold = 1f;
    public bool IsAlive { get; private set; } = true;
    public bool scaleShape = false;
    public Stats Stats { get; private set; }
    private TrajectoryRenderer _hitscanRenderer;

    private void Awake() {
        Stats = gameObject.AddComponent<Stats>();
        gameObject.AddComponent<Statusable>();
        _hitscanRenderer = GetComponent<TrajectoryRenderer>();
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

        if (_hitscanRenderer != null)
            _hitscanRenderer.Clear();
        DI.Get<IEntityManager>().Despawn(transform.parent.gameObject);
    }

    public void DrawTrajectory(Vector3[] points) {
        if (_hitscanRenderer == null) return;
        _hitscanRenderer.Build(points);
    }
}