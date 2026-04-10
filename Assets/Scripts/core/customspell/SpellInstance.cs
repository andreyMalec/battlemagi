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
    public ulong ObjectId => _authorityService.ObjectId;
    public GameObject Get => gameObject;

    public void Init(ISpellBind bind, IAuthorityService authorityService) {
        _initialized = true;
        Active.Add(this);
        Bind = bind;
        _authorityService = authorityService;

        Scale(bind.Context.Spell.scale, bind.Context.Lifetime);
        ParticleUtils.ApplyBeamShape(gameObject, bind.Context.Spell.beam);
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

        foreach (var rz in GetComponentsInChildren<AudioReverbZone>(true)) {
            rz.minDistance *= k;
            rz.maxDistance *= k;
        }
    }
}