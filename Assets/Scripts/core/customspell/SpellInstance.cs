using UnityEngine;

public interface ISpellBind {
    ISpellContext Context { get; }
    void Tick(float deltaTime);
}

public class SpellInstance : MonoBehaviour {
    public ISpellBind Bind { get; private set; }

    public void Init(ISpellBind bind) {
        Bind = bind;

        var k = bind.Context.Spell.zoneRadius;
        foreach (var ps in GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = bind.Context.Lifetime;
            if (!ps.main.loop) {
                if (main.startLifetime.constantMax > bind.Context.Lifetime)
                    main.startLifetime = bind.Context.Lifetime;
            }

            ParticleUtils.Scale(ps, k);
            ps.Play(true);
        }
    }

    void FixedUpdate() {
        if (Bind.Context.View.IsAlive)
            Bind.Tick(Time.deltaTime);
    }
}