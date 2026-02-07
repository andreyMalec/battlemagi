using System.Collections;
using UnityEngine;

public class SpellView : MonoBehaviour {
    public float beforeEndThreshold = 1f;
    public bool IsAlive { get; private set; } = true;

    public void Kill() {
        if (!IsAlive) return;
        IsAlive = false;

        foreach (var ps in GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

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

        Destroy(gameObject);
    }
}