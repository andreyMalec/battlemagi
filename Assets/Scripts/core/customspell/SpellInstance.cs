using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ISpellBind {
    ISpellContext Context { get; }
    void Tick(float deltaTime);
}

public class SpellInstance : MonoBehaviour, ITarget {
    public static readonly List<SpellInstance> Active = new();

    [SerializeField] private GameObject[] scale;
    [SerializeField] private ParticleSystem[] exclude;
    public ISpellBind Bind { get; private set; }
    private IAuthorityService _authorityService;
    private bool _initialized;

    public Vector3 Position => transform.position;
    public bool IsPlayer => false;
    public bool IsSpell => true;
    public bool IsAlive => Bind.Context.View.IsAlive;
    public OwnerId OwnerId => _authorityService.OwnerId;
    public GameObject Get => gameObject;

    public void Init(ISpellBind bind, IAuthorityService authorityService) {
        _initialized = true;
        Active.Add(this);
        Bind = bind;
        _authorityService = authorityService;

        Scale(bind.Context.Spell.scale, bind.Context.Lifetime);
    }

    void FixedUpdate() {
        if (!_initialized) return;
        if (IsAlive) {
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
        var scaleShape = GetComponent<SpellView>().scaleShape;
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true)) {
            if (exclude.Contains(ps))
                continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            if (!ps.main.loop) {
                if (Mathf.Approximately(main.startLifetime.constant, main.duration))
                    main.startLifetime = lifetime;
                main.duration = lifetime;
            }


            ParticleUtils.Scale(ps, k, scaleShape);
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