using System.Collections.Generic;
using UnityEngine;

public interface ISpellBind {
    ISpellContext Context { get; }
    void Tick(float deltaTime);
}

public class SpellInstance : MonoBehaviour {
    public static readonly List<SpellInstance> Active = new();

    [SerializeField] private GameObject[] scale;
    public ISpellBind Bind { get; private set; }

    private void OnEnable() {
        Active.Add(this);
    }

    public void Init(ISpellBind bind) {
        Bind = bind;

        var k = bind.Context.Spell.scale;
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

        foreach (var go in scale) {
            go.transform.localScale = Vector3.one * k;
        }

        foreach (var tr in GetComponentsInChildren<TrailRenderer>()) {
            tr.widthMultiplier = k;
        }
    }

    void FixedUpdate() {
        if (Bind.Context.View.IsAlive) {
            Bind.Tick(Time.deltaTime);
            return;
        }

        Active.Remove(this);
    }
}