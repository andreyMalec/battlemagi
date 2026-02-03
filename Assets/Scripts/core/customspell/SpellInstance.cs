using UnityEngine;

public class SpellInstance : MonoBehaviour {
    [SerializeField] private GameObject[] scaleWithRadius;
    [SerializeField] private ParticleSystem[] scaleParticleWithRadius;

    public SpellBind Bind { get; private set; }

    public void Init(SpellBind bind) {
        Bind = bind;
        var k = bind.Context.Data.zoneRadius;

        foreach (var go in scaleWithRadius)
            go.transform.localScale *= k;

        foreach (var ps in scaleParticleWithRadius) {
            ParticleUtils.Scale(ps, k);
        }
    }

    void FixedUpdate() {
        Bind.Tick(Time.deltaTime);
    }
}