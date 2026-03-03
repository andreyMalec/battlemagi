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
    private SpellSystemEvent _event;

    private bool _canCast = false;
    public override bool CanCast => _canCast;
    public override Vector3 Origin => spawnPos.position;
    public override Vector3 Direction => spawnPos.forward;

    private new void Awake() {
        base.Awake();
        foreach (var ps in onAttackParticles)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var ps in onAttackSounds)
            ps.Stop();
        _timer = cooldown - activationDelay;
        _view = GetComponent<SpellView>();
        _event = GetComponentInParent<SpellSystemEvent>();
    }

    private void FixedUpdate() {
        _timer += Time.fixedDeltaTime;
        _canCast = _timer >= cooldown && _view.IsAlive;
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
        base.Cast(spell);
    }

    public override void Cast(SpellDefinition spell, ITarget target) {
        _event.OnAttack(this);
        base.Cast(spell, target);
    }
}