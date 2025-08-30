using UnityEngine;
using System.Collections;
using FullOpaqueVFX;

public class SpellManager : MonoBehaviour {
    public Transform spellCastPoint;
    public CameraShake cameraShake;

    public IEnumerator CastSpell(SpellData currentSpell) {
        if (!Application.isPlaying) yield break;

        yield return new WaitForSeconds(currentSpell.castTime);

        Vector3 spawnPosition = spellCastPoint.position;
        Quaternion spawnRotation = spellCastPoint.rotation;

        if (currentSpell.spawnOnGround) {
            spawnPosition = GetGroundPosition(spellCastPoint.position);
        }

        GameObject mainSpell = currentSpell.SpawnEffect(currentSpell.mainSpellPrefab, spawnPosition, spawnRotation);
        if (mainSpell != null) {
            if (mainSpell.TryGetComponent<SpellProjectile>(out var projectile))
                projectile.Initialize(currentSpell);
            mainSpell.SetActive(true);
            // MainSpellManager mainSpellManager = mainSpell.GetComponent<MainSpellManager>();
            // if (mainSpellManager != null)
            // {
            //     mainSpellManager.SetImpactShakeStrength(currentSpell.shakeStrengthImpact);
            //     mainSpellManager.SetImpactShakeDuration(currentSpell.shakeDurationImpact);
            //     mainSpellManager.SetTarget(target);
            //
            //     if (currentSpell.spellTracking)
            //         mainSpellManager.EnableTracking();
            // }
            PlayParticleSystem(mainSpell);
        }

        // 3️⃣ Gestion du Spell Burst
        if (currentSpell.spellBurstPrefab != null) {
            Vector3 burstPosition = spellCastPoint.position;
            GameObject spellBurst =
                currentSpell.SpawnEffect(currentSpell.spellBurstPrefab, burstPosition, Quaternion.identity);
            if (spellBurst != null) {
                spellBurst.SetActive(true);
                PlayParticleSystem(spellBurst);
                if (currentSpell.shakeEnabled && cameraShake != null) {
                    cameraShake.Shake(currentSpell.shakeStrengthBurst, currentSpell.shakeDurationBurst);
                }

                StartCoroutine(DestroyAfterParticles(spellBurst));
            }
        }
    }

    private Vector3 GetGroundPosition(Vector3 originPos) {
        RaycastHit hit;
        Vector3 rayStart = originPos + Vector3.up * 10f;
        int terrainLayer = LayerMask.NameToLayer("Terrain");
        int terrainLayerMask = 1 << terrainLayer;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, terrainLayerMask)) {
            return hit.point;
        }

        return originPos;
    }

    private void AdjustParticleLifetime(GameObject spellObject, float lifetime) {
        if (spellObject == null) return;
        ParticleSystem[] particleSystems = spellObject.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems) {
            var mainModule = ps.main;
            if (!mainModule.loop) {
                mainModule.startLifetime = lifetime;
            }
        }
    }

    private void PlayParticleSystem(GameObject obj) {
        if (obj == null) return;
        ParticleSystem[] psArray = obj.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in psArray) {
            ps.Play();
        }
    }

    private IEnumerator DestroyAfterParticles(GameObject obj) {
        if (obj == null) yield break;
        ParticleSystem[] psArray = obj.GetComponentsInChildren<ParticleSystem>();
        yield return new WaitUntil(() => System.Array.TrueForAll(psArray, ps => ps == null || !ps.IsAlive(true)));
        Destroy(obj);
    }
}