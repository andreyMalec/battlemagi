using UnityEngine;

public class SpellInstance : MonoBehaviour {
    [SerializeField] private GameObject[] scaleWithRadius;
    [SerializeField] private ParticleSystem[] scaleParticleWithRadius;

    private SpellBind _bind;

    public void Init(SpellBind bind) {
        _bind = bind;
        var k = bind.Context.Data.zoneRadius;

        foreach (var go in scaleWithRadius)
            go.transform.localScale *= k;

        foreach (var ps in scaleParticleWithRadius) {
            ParticleUtils.Scale(ps, k);
        }
    }

    void FixedUpdate() {
        _bind.Tick(Time.deltaTime);
    }
}