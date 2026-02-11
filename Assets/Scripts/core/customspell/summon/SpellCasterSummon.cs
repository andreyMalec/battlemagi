using System;
using UnityEngine;

public class SpellCasterSummon : SpellCaster {
    [SerializeField] private Transform spawnPos;
    [SerializeField] private float activationDelay = 0f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private ParticleSystem[] onAttackParticles;
    [SerializeField] private AudioSource[] onAttackSounds;

    private ITarget _target;
    private SpellDefinition _spell;
    private float _timer;
    private SpellView _view;

    private bool _canCast = false;
    public override bool CanCast => _canCast;
    public override Vector3 Origin => spawnPos.position;
    public override Vector3 Direction => spawnPos.forward;

    private void Awake() {
        foreach (var ps in onAttackParticles)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var ps in onAttackSounds)
            ps.Stop();
        _timer = cooldown - activationDelay;
        _view = GetComponent<SpellView>();
    }

    private void FixedUpdate() {
        _timer += Time.fixedDeltaTime;
        _canCast = _timer >= cooldown && _view.IsAlive;
    }

    private void OnAttack() {
        foreach (var ps in onAttackParticles)
            ps.Play();
        foreach (var ps in onAttackSounds)
            ps.Play();
        _timer = 0f;
    }

    public override void Cast(SpellDefinition spell) {
        OnAttack();
        base.Cast(spell);
    }

    public override void Cast(SpellDefinition spell, ITarget target) {
        OnAttack();
        _target = target;
        _spell = spell;
        var forward = (spawnPos.position - target.Position).normalized;
        var context = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = target.Position,
            rotation = Quaternion.LookRotation(forward, Vector3.up),
            forward = forward,
            caster = this
        };
        Cast(context);
    }
}