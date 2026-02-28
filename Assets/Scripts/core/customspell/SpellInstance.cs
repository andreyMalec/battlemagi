using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ISpellBind {
    ISpellContext Context { get; }
    void Tick(float deltaTime);
}

public class SpellInstance : MonoBehaviour {
    public static readonly List<SpellInstance> Active = new();

    [SerializeField] private GameObject[] scale;
    [SerializeField] private ParticleSystem[] exclude;
    public ISpellBind Bind { get; private set; }
    private IAuthorityService _authorityService;
    private bool _initialized;

    public void Init(ISpellBind bind, IAuthorityService authorityService) {
        _initialized = true;
        Active.Add(this);
        Bind = bind;
        _authorityService = authorityService;

        Scale(bind.Context.Spell.scale, bind.Context.Lifetime);
    }

    void FixedUpdate() {
        if (!_initialized) return;
        if (Bind.Context.View.IsAlive) {
            if (_authorityService.IsServer)
                Bind.Tick(Time.deltaTime);
            return;
        }

        Active.Remove(this);
    }

    public void Kill() {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void FadeOutAudio() {
        var view = GetComponent<SpellView>();
        var duration = view.beforeEndThreshold;
        var fade = view.GetComponent<AudioSourcesFadeOut>();
        if (!fade) fade = view.gameObject.AddComponent<AudioSourcesFadeOut>();
        fade.Begin(duration);
    }

    public void RemoveVisual() {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        foreach (var mesh in GetComponentsInChildren<MeshRenderer>()) {
            mesh.enabled = false;
        }

        foreach (var lt in GetComponentsInChildren<Light>()) {
            lt.enabled = false;
        }

        foreach (var cl in GetComponentsInChildren<Collider>()) {
            cl.enabled = false;
        }
    }

    public void Scale(float k, float lifetime) {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true)) {
            if (exclude.Contains(ps))
                continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = lifetime;
            if (!ps.main.loop) {
                if (main.startLifetime.constantMax > lifetime)
                    main.startLifetime = lifetime;
            }

            ParticleUtils.Scale(ps, k);
            ps.Play(true);
        }

        foreach (var go in scale) {
            go.transform.localScale = Vector3.one * k;
        }

        foreach (var tr in GetComponentsInChildren<TrailRenderer>(true)) {
            tr.widthMultiplier = k;
        }
    }
}