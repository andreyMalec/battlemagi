using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ISpellBind {
    ISpellContext Context { get; }
    void Tick(float deltaTime);
}

public class SpellInstance : MonoBehaviour, ITarget {
    public static readonly List<SpellInstance> Active = new();
    private static SpellInstanceTicker _ticker;

    [SerializeField] private GameObject[] scale;
    [SerializeField] private ParticleSystem[] exclude;
    public ISpellBind Bind { get; private set; }
    private IAuthorityService _authorityService;
    private bool _initialized;
    private SpellView _view;
    private int _activeIndex = -1;

    public Vector3 Position => transform.position;
    public bool IsPlayer => false;
    public bool IsSpell => true;
    public bool IsAlive => _view.IsAlive;
    public OwnerId OwnerId => _authorityService.OwnerId;
    public ulong ObjectId => _authorityService.ObjectId;
    public GameObject Get => gameObject;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() {
        Active.Clear();
        _ticker = null;
    }

    public void Init(ISpellBind bind, IAuthorityService authorityService) {
        Bind = bind;
        _authorityService = authorityService;
        _view = bind.Context.View;
        _initialized = true;
        RegisterActive();

        Scale(bind.Context.Spell.scale, bind.Context.Lifetime);
        ParticleUtils.ApplyBeamShape(gameObject, bind.Context.Spell.beam);
    }

    private void OnDestroy() {
        UnregisterActive();
    }

    private void TickFixed(float deltaTime) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.InstanceTick);
        if (!_initialized) return;
        if (IsAlive) {
            if (_authorityService.IsServer)
                Bind.Tick(deltaTime);
            return;
        }

        UnregisterActive();
    }

    public void Kill() {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (_authorityService.IsServer) {
            SpellInstanceLimiter.Unregister(OwnerId, Bind.Context.Spell, gameObject);
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
        var scaleShape = _view.scaleShape;
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

        foreach (var rz in GetComponentsInChildren<AudioReverbZone>(true)) {
            rz.minDistance *= k;
        }
    }


    private void OnDrawGizmos() {
        if (Bind == null) return;
        var beam = Bind.Context.Spell.beam;
        var zone = Bind.Context.Spell.zone;
        var c = Color.red;
        c.a = 0.5f;
        Gizmos.color = c;
        if (beam != null) {
            if (beam.shapeType is BeamShapeType.Cone)
                DrawConeGizmos(beam);
            else
                Gizmos.DrawRay(transform.position, transform.forward * beam.MaxLength);
        }

        if (zone != null) {
            Gizmos.DrawSphere(transform.position, Bind.Context.Spell.scale);
        }
    }

    private void DrawConeGizmos(BeamDefinition beam) {
        var startCenter = transform.position;
        var forward = transform.forward;
        var up = transform.up;
        var right = transform.right;
        var length = beam.coneLength;
        var endCenter = startCenter + forward * length;
        var startRadius = Mathf.Max(0f, beam.coneRadius);
        var endRadius = ParticleUtils.GetConeEndRadius(beam);
        var midCenter = Vector3.Lerp(startCenter, endCenter, 0.5f);
        var midRadius = Mathf.Lerp(startRadius, endRadius, 0.5f);

        DrawCircleGizmos(startCenter, right, up, startRadius);
        DrawCircleGizmos(midCenter, right, up, midRadius);
        DrawCircleGizmos(endCenter, right, up, endRadius);

        DrawConeSide(startCenter, endCenter, right, startRadius, endRadius);
        DrawConeSide(startCenter, endCenter, -right, startRadius, endRadius);
        DrawConeSide(startCenter, endCenter, up, startRadius, endRadius);
        DrawConeSide(startCenter, endCenter, -up, startRadius, endRadius);
    }

    private static void DrawConeSide(
        Vector3 startCenter, Vector3 endCenter, Vector3 axis, float startRadius, float endRadius
    ) {
        Gizmos.DrawLine(startCenter + axis * startRadius, endCenter + axis * endRadius);
    }

    private static void DrawCircleGizmos(Vector3 center, Vector3 axisX, Vector3 axisY, float radius) {
        if (radius <= 0.0001f)
            return;

        const int segments = 24;
        var prev = center + axisX * radius;
        for (var i = 1; i <= segments; i++) {
            var angle = i / (float)segments * Mathf.PI * 2f;
            var next = center + (axisX * Mathf.Cos(angle) + axisY * Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private void RegisterActive() {
        EnsureTicker();
        if (_activeIndex >= 0)
            return;

        _activeIndex = Active.Count;
        Active.Add(this);
        _ticker.enabled = true;
    }

    private void UnregisterActive() {
        var index = _activeIndex;
        if (index < 0)
            return;

        _activeIndex = -1;

        if ((uint)index >= (uint)Active.Count)
            return;

        if (Active[index] != this) {
            index = Active.IndexOf(this);
            if (index < 0)
                return;
        }

        RemoveAt(index);
        if (_ticker != null)
            _ticker.enabled = Active.Count > 0;
    }

    private static void TickActive(float deltaTime) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.ActiveTick);
        SpellMetrics.RecordActiveSpells(Active.Count);
        for (var i = Active.Count - 1; i >= 0; i--) {
            var instance = Active[i];
            if (instance == null) {
                RemoveAt(i);
                continue;
            }

            instance.TickFixed(deltaTime);
        }

        SpellMetrics.FlushIfNeeded();
    }

    private static void RemoveAt(int index) {
        if ((uint)index >= (uint)Active.Count)
            return;

        var lastIndex = Active.Count - 1;
        var removed = Active[index];
        var last = Active[lastIndex];
        if (index != lastIndex) {
            Active[index] = last;
            if (last != null)
                last._activeIndex = index;
        }

        Active.RemoveAt(lastIndex);
        if (removed != null)
            removed._activeIndex = -1;
    }

    private static void EnsureTicker() {
        if (_ticker != null)
            return;

        _ticker = FindAnyObjectByType<SpellInstanceTicker>();
        if (_ticker != null)
            return;

        var go = new GameObject(nameof(SpellInstanceTicker));
        DontDestroyOnLoad(go);
        _ticker = go.AddComponent<SpellInstanceTicker>();
        _ticker.enabled = false;
    }

    private sealed class SpellInstanceTicker : MonoBehaviour {
        private void FixedUpdate() {
            using var _ = SpellMetrics.Measure(SpellMetricSection.TickerFixedUpdate);
            if (Active.Count == 0) {
                enabled = false;
                return;
            }

            TickActive(Time.fixedDeltaTime);
        }

        private void OnDestroy() {
            if (_ticker == this)
                _ticker = null;
        }
    }
}