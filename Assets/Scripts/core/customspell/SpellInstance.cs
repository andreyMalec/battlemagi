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
    private static readonly Dictionary<int, List<SpellInstance>> AudioGroups = new();
    private static Transform _cachedListener;

    [SerializeField] private GameObject[] scale;
    [SerializeField] private ParticleSystem[] exclude;
    public ISpellBind Bind { get; private set; }
    private ISpellContext _context;
    private SpellDefinition _spell;
    private IAuthorityService _authorityService;
    private bool _initialized;
    private bool _isServer;
    private bool _dieWithCaster;
    private SpellView _view;
    private int _activeIndex = -1;
    private int _audioGroupKey;
    private AudioSource[] _audioSources;

    public Vector3 Position => transform.position;
    public bool IsPlayer => false;
    public bool IsSpell => true;
    public bool IsAlive => _view != null && _view.IsAlive;
    public ParticipantId OwnerId => _authorityService.OwnerId;
    public ulong ObjectId => _authorityService.ObjectId;
    public bool CanGet => gameObject != null;
    public GameObject Get => gameObject;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() {
        Active.Clear();
        AudioGroups.Clear();
        _cachedListener = null;
        _ticker = null;
    }

    public void Init(ISpellBind bind, IAuthorityService authorityService) {
        Bind = bind;
        _context = bind.Context;
        _spell = _context.Spell;
        _authorityService = authorityService;
        _isServer = authorityService != null && authorityService.IsServer;
        _dieWithCaster = _spell.dieWithCaster;
        _view = _context.View;
        _audioGroupKey = GetAudioGroupKey(_spell);
        _audioSources = GetComponentsInChildren<AudioSource>(true);
        var stats = _context.Caster.GetComponentInParent<Stats>();
        if (stats != null) {
            var spellDmg = stats.GetFinal(StatType.SpellDamage);
            if (!Mathf.Approximately(spellDmg, 1f))
                _view.Stats.AddModifier(StatType.SpellDamage, spellDmg);
        }
        _initialized = true;
        RegisterActive();
        RegisterAudio();

        Scale(_spell.scale, _context.Lifetime);
        ParticleUtils.ApplyBeamShape(gameObject, _spell.beam);
    }

    private void OnDestroy() {
        UnregisterAudio();
        UnregisterActive();
    }

    private void TickFixed(float deltaTime) {
        using var _ = SpellMetrics.Measure(SpellMetricSection.InstanceTick);
        if (!_initialized) return;
        if (_view == null || !_view.IsAlive) {
            UnregisterActive();
            return;
        }

        if (!_isServer)
            return;

        if (_dieWithCaster && _context.Caster.IsDead)
            _view.Kill(_context);

        Bind.Tick(deltaTime);
    }

    public void Kill() {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (Mathf.Approximately(ps.main.startLifetime.constant, ps.main.duration)) {
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.particleCount];
                int count = ps.GetParticles(particles);

                for (int i = 0; i < count; i++) {
                    if (particles[i].remainingLifetime > 1)
                        particles[i].remainingLifetime = 1f; // уменьшаем оставшуюся жизнь
                }

                ps.SetParticles(particles, count);
            }
        }

        if (_authorityService != null && _authorityService.IsServer) {
            SpellInstanceLimiter.Unregister(OwnerId.Value, Bind.Context.Spell, gameObject);
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
        var scaleShape = GetComponent<SpellView>().scaleShape; // _view инициализируется только на сервере
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
            DrawZoneGizmos(zone);
        }
    }

    private void DrawZoneGizmos(ZoneDefinition zone) {
        if (zone.shapeType is ZoneShapeType.Plate) {
            var side = Bind.Context.Spell.scale * 2f;
            var height = side * 0.1f;
            var previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, new Vector3(side, height, side));
            Gizmos.matrix = previousMatrix;
            return;
        }

        Gizmos.DrawSphere(transform.position, Bind.Context.Spell.scale);
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

            if (!instance._isServer && instance._view != null && instance._view.IsAlive)
                continue;

            instance.TickFixed(deltaTime);
        }

        UpdateAudioGroups();

        SpellMetrics.FlushIfNeeded();
    }

    private static void UpdateAudioGroups() {
        if (AudioGroups.Count == 0)
            return;

        if (!TryGetListenerPosition(out var listenerPosition))
            return;

        foreach (var pair in AudioGroups) {
            var group = pair.Value;
            if (group == null || group.Count == 0)
                continue;

            SpellInstance nearest0 = null;
            SpellInstance nearest1 = null;
            SpellInstance nearest2 = null;
            var d0 = float.PositiveInfinity;
            var d1 = float.PositiveInfinity;
            var d2 = float.PositiveInfinity;

            for (var i = group.Count - 1; i >= 0; i--) {
                var instance = group[i];
                if (instance == null || !instance._initialized) {
                    group.RemoveAt(i);
                    continue;
                }

                instance.SetAudioMuted(true);
                var sqrDistance = (instance.transform.position - listenerPosition).sqrMagnitude;
                if (sqrDistance >= d2)
                    continue;

                if (sqrDistance < d0) {
                    d2 = d1;
                    nearest2 = nearest1;
                    d1 = d0;
                    nearest1 = nearest0;
                    d0 = sqrDistance;
                    nearest0 = instance;
                    continue;
                }

                if (sqrDistance < d1) {
                    d2 = d1;
                    nearest2 = nearest1;
                    d1 = sqrDistance;
                    nearest1 = instance;
                    continue;
                }

                d2 = sqrDistance;
                nearest2 = instance;
            }

            nearest0?.SetAudioMuted(false);
            nearest1?.SetAudioMuted(false);
            nearest2?.SetAudioMuted(false);
        }
    }

    private static bool TryGetListenerPosition(out Vector3 listenerPosition) {
        if (Player.local != null) {
            listenerPosition = Player.local.transform.position;
            return true;
        }

        if (_cachedListener == null || !_cachedListener.gameObject.activeInHierarchy) {
            var listener = FindAnyObjectByType<AudioListener>();
            _cachedListener = listener != null ? listener.transform : null;
        }

        if (_cachedListener != null) {
            listenerPosition = _cachedListener.position;
            return true;
        }

        listenerPosition = default;
        return false;
    }

    private void RegisterAudio() {
        if (_audioSources == null || _audioSources.Length == 0)
            return;

        if (!AudioGroups.TryGetValue(_audioGroupKey, out var group)) {
            group = new List<SpellInstance>(4);
            AudioGroups[_audioGroupKey] = group;
        }

        group.Add(this);
        SetAudioMuted(false);
    }

    private void UnregisterAudio() {
        if (_audioSources == null || _audioSources.Length == 0)
            return;

        if (!AudioGroups.TryGetValue(_audioGroupKey, out var group))
            return;

        group.Remove(this);
        if (group.Count == 0)
            AudioGroups.Remove(_audioGroupKey);

        SetAudioMuted(false);
    }

    private void SetAudioMuted(bool muted) {
        if (_audioSources == null)
            return;

        for (var i = 0; i < _audioSources.Length; i++) {
            var source = _audioSources[i];
            if (source == null)
                continue;
            if (source.mute == muted)
                continue;
            source.mute = muted;
        }
    }

    private static int GetAudioGroupKey(SpellDefinition spell) {
        var prefabId = spell.coreType switch {
            CoreType.Projectile => (int)spell.projectile.prefabId,
            CoreType.Zone => (int)spell.zone.prefabId,
            CoreType.Beam => (int)spell.beam.prefabId,
            CoreType.Self => (int)spell.self.prefabId,
            CoreType.Summon => (int)spell.summon.prefabId,
            _ => -1
        };

        return ((int)spell.coreType << 16) ^ prefabId;
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