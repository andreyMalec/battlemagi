using System;
using System.Collections;
using Unity.Netcode.Components;
using UnityEngine;

public class SpellCasterPlayerAnimator : MonoBehaviour {
    private static readonly int Invocation = Animator.StringToHash("Invocation");
    private static readonly int CastWaiting = Animator.StringToHash("Cast Waiting");
    private static readonly int CastSpeed = Animator.StringToHash("CastSpeed");
    private static readonly int CastWaitingIndex = Animator.StringToHash("CastWaitingIndex");
    private static readonly int CancelChanneling = Animator.StringToHash("CancelChanneling");
    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int IdleIndex = Animator.StringToHash("IdleIndex");

    [SerializeField] private Vector2 idleTimeRange = new Vector2(15f, 30f);
    [SerializeField] private Vector2 idleIndexRange = new Vector2(0f, 2f);

    private MeshController _meshController;
    private Animator _animator;
    private Animator _handsAnimator;
    private SpellDefinition _castedSpell;
    public SpellDefinition preparedSpell;
    private SpellCasterPlayer _caster;
    private Stats _stats;

    private NetworkAnimator _networkAnimator;

    private bool _isOwner;
    private float _idleTimer;
    private float _idleWait;

    private void Awake() {
        _caster = GetComponent<SpellCasterPlayer>();
        _stats = GetComponent<Stats>();
        _idleWait = UnityEngine.Random.Range(idleTimeRange.x, idleTimeRange.y);
    }

    private void Update() {
        if (!_isOwner) return;

        if (_castedSpell != null || preparedSpell != null) {
            CastWaitingAnim(true, _castedSpell?.castWaitingIndex ?? preparedSpell?.castWaitingIndex ?? 0);
            _idleTimer = 0f;
        } else {
            CastWaitingAnim(false);
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _idleWait) {
                _handsAnimator?.SetInteger(IdleIndex,
                    UnityEngine.Random.Range((int)idleIndexRange.x, (int)idleIndexRange.y + 1));
                _handsAnimator?.SetTrigger(Idle);
                _idleTimer = 0f;
                _idleWait = UnityEngine.Random.Range(idleTimeRange.x, idleTimeRange.y);
            }
        }
    }

    public void BindAvatar(MeshController mc, NetworkAnimator na, Animator a, bool isOwner) {
        _isOwner = isOwner;
        if (_meshController != null && isOwner)
            _meshController.OnCast -= OnSpellCasted;

        _meshController = mc;
        _animator = a;
        _networkAnimator = na;

        if (_meshController != null && isOwner)
            _meshController.OnCast += OnSpellCasted;
    }

    public void BindHandsAnimator(Animator handsAnimator) {
        _handsAnimator = handsAnimator;
    }

    private void OnSpellCasted(bool _) {
        if (_castedSpell == null) return;
        _caster.Cast(_castedSpell);
        _castedSpell = null;
    }

    public void CancelAnimate() {
        _castedSpell = null;
        _networkAnimator.SetTrigger(CancelChanneling);
        _handsAnimator?.SetTrigger(CancelChanneling);
        CastWaitingAnim(false);
    }

    public void AnimateCast(SpellDefinition spell) {
        if (!_isOwner) return;
        CastWaitingAnim(false);
        _castedSpell = spell;
        if (spell.invocationIndex <= 0)
            OnSpellCasted(true);
        else
            StartCoroutine(Animate(spell));
    }

    public void CastWaitingAnim(bool waiting, int index = 0) {
        if (!_isOwner) return;
        _animator.SetBool(CastWaiting, waiting);
        _handsAnimator?.SetBool(CastWaiting, waiting);
        if (waiting) {
            _animator.SetFloat(CastWaitingIndex, index);
            _handsAnimator?.SetFloat(CastWaitingIndex, index);
        } else
            _castedSpell = null;
    }

    private IEnumerator Animate(SpellDefinition spell) {
        if (!_isOwner) yield break;
        var castSpeed = _stats?.GetFinal(StatType.CastSpeed) ?? 1f;
        _animator.SetFloat(CastSpeed, castSpeed);
        _handsAnimator?.SetFloat(CastSpeed, castSpeed);
        _animator.SetFloat(Invocation, spell.invocationIndex);
        _handsAnimator?.SetFloat(Invocation, spell.invocationIndex);
        yield return new WaitForSeconds(0.1f);
        _animator.SetFloat(Invocation, 0);
        _handsAnimator?.SetFloat(Invocation, 0);
    }
}