using System;
using UnityEngine;

public class SpellCasterSummon : SpellCaster {
    [SerializeField] private Transform spawnPos;
    [SerializeField] private float activationDelay = 0f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private ParticleSystem[] onAttackParticles;
    [SerializeField] private AudioSource[] onAttackSounds;
    [SerializeField] private AudioSource[] onActivateSounds;

    private ITarget _target;
    private SpellDefinition _spell;
    private float _timer;
    private float _activationTimer;
    private SpellInstance _instance;
    private SpellSystemEvent _event;

    private bool _canCast = false;
    public override bool CanCast => _canCast;
    public override Vector3 Origin => spawnPos.position;
    public override Vector3 Direction => spawnPos.forward;

    public override bool IsPlayer => false;
    public override bool IsSpell => true;

    private new void Awake() {
        base.Awake();
        foreach (var ps in onAttackParticles)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var ps in onAttackSounds)
            ps.Stop();
        foreach (var ps in onActivateSounds)
            ps.Stop();
        _timer = cooldown - activationDelay;
        _instance = GetComponent<SpellInstance>();
        _event = GetComponentInParent<SpellSystemEvent>();
    }

    private void FixedUpdate() {
        _timer += Time.fixedDeltaTime;
        _activationTimer += Time.fixedDeltaTime;
        if (_activationTimer >= activationDelay && _activationTimer - Time.fixedDeltaTime < activationDelay) {
            foreach (var ps in onActivateSounds)
                ps.Play();
        }

        _canCast = _timer >= cooldown && _instance.IsAlive;
    }

    public void OnAttack() {
        foreach (var ps in onAttackParticles)
            ps.Play();
        foreach (var ps in onAttackSounds)
            ps.Play();
        _timer = 0f;
    }

    public override void Cast(SpellDefinition spell) {
        _event.OnAttack(this);
        _instance.Bind.Context.SendEvent(new OnSummonAttackEvent(null));
        base.Cast(spell);
    }

    public override void Cast(SpellDefinition spell, ITarget target) {
        _event.OnAttack(this);
        _instance.Bind.Context.SendEvent(new OnSummonAttackEvent(target));
        base.Cast(spell, target);
    }
}