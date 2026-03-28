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

    private MeshController _meshController;
    private Animator _animator;
    private SpellDefinition _spell;
    private SpellCasterPlayer _caster;
    private Stats _stats;

    private NetworkAnimator _networkAnimator;

    public Transform ikHand;
    public Transform ikHandRight;
    private Vector3 _ikPos;
    private Quaternion _ikRot;

    private void Awake() {
        _caster = GetComponent<SpellCasterPlayer>();
        _stats = GetComponent<Stats>();
    }

    public void BindAvatar(MeshController mc, NetworkAnimator na, Animator a, bool isOwner) {
        if (_meshController != null && isOwner)
            _meshController.OnCast -= OnSpellCasted;

        _meshController = mc;
        _animator = a;
        _networkAnimator = na;

        if (_meshController != null && isOwner)
            _meshController.OnCast += OnSpellCasted;

        if (isOwner) {
            _ikPos = ikHand.localPosition;
            _ikRot = ikHand.localRotation;
        }
    }

    private void OnSpellCasted(bool _) {
        if (_spell == null) return;
        _caster.Cast(_spell);
        _spell = null;
    }

    public void CancelSpellChanneling() {
        _networkAnimator.SetTrigger(CancelChanneling);
    }

    public void AnimateCast(SpellDefinition spell) {
        _spell = spell;
        if (spell.invocationIndex <= 0)
            OnSpellCasted(true);
        else
            StartCoroutine(Animate(spell));
    }

    public void CastWaitingAnim(bool waiting, int index = 0) {
        _animator.SetBool(CastWaiting, waiting);
        if (waiting)
            _animator.SetFloat(CastWaitingIndex, index);
        if (index == 0) {
            _meshController.leftHand.weight = 1;
            _meshController.rightHand.weight = 0;
            if (waiting) {
                ikHand.localPosition = new Vector3(-0.55f, -0.24f, 0.44f);
                ikHand.localRotation = Quaternion.Euler(0, -90, 0);
            } else {
                ikHand.localPosition = _ikPos;
                ikHand.localRotation = _ikRot;
            }

            ikHandRight.localPosition = ikHand.localPosition;
            ikHandRight.localRotation = ikHand.localRotation;
        }

        if (index == 1) {
            _meshController.leftHand.weight = 1f; //TODO
            _meshController.rightHand.weight = 1f;
            if (waiting) {
                ikHand.localPosition = new Vector3(0.14f, -0.225f, 0.23f);
                ikHand.localRotation = Quaternion.Euler(-192f, -74.6f, -108f);
                ikHandRight.localPosition = new Vector3(0.08f, -0.178f, 0.252f);
                ikHandRight.localRotation = Quaternion.Euler(-210f, -82f, -96f);
            } else {
                ikHand.localPosition = _ikPos;
                ikHand.localRotation = _ikRot;
                ikHandRight.localPosition = ikHand.localPosition;
                ikHandRight.localRotation = ikHand.localRotation;
            }
        }
    }

    private IEnumerator Animate(SpellDefinition spell) {
        _animator.SetFloat(CastSpeed, _stats?.GetFinal(StatType.CastSpeed) ?? 1f);
        _animator.SetFloat(Invocation, spell.invocationIndex);
        yield return new WaitForSeconds(0.1f);
        _animator.SetFloat(Invocation, 0);
    }
}