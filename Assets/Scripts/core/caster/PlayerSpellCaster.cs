using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Voice;

[RequireComponent(typeof(SpellManager))]
public class PlayerSpellCaster : NetworkBehaviour {
    [SerializeField] private AudioSource noManaSound;
    [SerializeField] private AudioSource disabledSound;
    private NetworkStatSystem _statSystem;
    private StatusEffectManager _effectManager;
    private MeshController _meshController;
    private Mouth _mouth;
    private PlayerAnimator _playerAnimator;
    private SpellManager _spellManager;

    public KeyCode spellCastKey = KeyCode.Mouse0;
    public KeyCode spellCancelKey = KeyCode.Mouse1;

    [HideInInspector] public bool channeling = false;

    public float manaRestore = 5f;
    [SerializeField] private float manaRestoreTickInterval = 0.5f;
    public float maxMana = 100;
    public NetworkVariable<float> mana = new();

    [SerializeField] private StatusEffectData primalManaStatus;
    public NetworkVariable<float> primalMana = new();

    private Coroutine _channelingCoroutine;

    private readonly PlayerSpellCastingState _state = new();
    private PlayerSpellManaController _manaController;
    private readonly PlayerSpellInput _input = new();
    private PlayerSpellRecognitionController _recognition;

    private void Awake() {
        _statSystem = GetComponent<NetworkStatSystem>();
        _mouth = GetComponent<Mouth>();
        _playerAnimator = GetComponent<PlayerAnimator>();
        _recognition = new PlayerSpellRecognitionController(_mouth);
        _effectManager = GetComponent<StatusEffectManager>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) {
            mana.Value = maxMana;
            primalMana.Value = 0f;
        }

        _manaController =
            new PlayerSpellManaController(mana, primalMana, maxMana, manaRestoreTickInterval, manaRestore, _statSystem);

        if (!IsOwner) return;

        _input.CastKey = spellCastKey;
        _input.CancelKey = spellCancelKey;

        if (_mouth == null)
            Debug.Log($"[Mouth] is null on Player_{OwnerClientId}");

        _recognition.Initialize(OwnerClientId, SpeechToTextHolder.Instance.Language);
    }

    public void BindAvatar(MeshController mc) {
        if (_meshController != null && IsOwner)
            _meshController.OnCast -= OnSpellCasted;

        _meshController = mc;

        if (_meshController != null && IsOwner)
            _meshController.OnCast += OnSpellCasted;
    }

    private void Start() {
        if (!IsOwner) return;
        _spellManager = GetComponent<SpellManager>();
        _mouth.OnMouthClose += OnMouthClose;
    }

    private void FixedUpdate() {
        if (!IsServer) return;
        _manaController.ServerTick(Time.deltaTime);
        if (IsPrimalManaLocked())
            _effectManager.AddEffect(OwnerClientId, primalManaStatus);
    }

    private void Update() {
        if (!IsOwner) return;

        HandleSpellKeys();
        HandleSpellCasting();

        _recognition.CanSpeak(!_state.CastWaiting && !_state.Channeling);
    }

    private bool IsPrimalManaLocked() {
        return primalMana.Value > 0f;
    }

    private void CancelSpell() {
        _spellManager.CancelSpell(_state.ChannelingElapsed);
        _state.SpellEcho = null;
    }

    private void CastSpell() {
        var spell = _state.SpellToCast();
        if (spell == null) return;

        if (spell.isChanneling) {
            _state.Channeling = true;
            channeling = true;
            if (_channelingCoroutine != null)
                StopCoroutine(_channelingCoroutine);
            _channelingCoroutine = StartCoroutine(Channel(spell));
        } else if (_state.EchoCount == spell.echoCount) {
            SpendManaServerRpc(_manaController.CostPerSecond(spell));
        }

        StartCoroutine(_playerAnimator.CastSpell(spell));
        StartCoroutine(_spellManager.CastSpell(spell));

        FirebaseAnalytic.Instance.SendEvent("SpellCasted", new Dictionary<string, object> {
            { "name", spell.name }
        });
    }

    private void OnSpellCasted(bool _) {
        if (_state.SpellEcho == null) return;
        _state.EchoCount--;
        if (_state.EchoCount >= 0)
            StartCoroutine(SpellEcho(_state.SpellEcho));
        else
            _state.SpellEcho = null;
    }

    private IEnumerator Channel(SpellData spell) {
        _state.ChannelingElapsed = 0f;
        var costPerSecond = _manaController.CostPerSecond(spell);
        while (_state.ChannelingElapsed < spell.channelDuration) {
            if (!_state.Channeling)
                yield break;

            var dt = Mathf.Min(manaRestoreTickInterval, spell.channelDuration - _state.ChannelingElapsed);
            var costPerTick = costPerSecond * dt;
            if (_manaController.CanSpendForChannelTick(spell, dt)) {
                SpendManaServerRpc(costPerTick);
            } else {
                AddPrimalManaServerRpc(PrimalManaMissing(costPerTick));
                _playerAnimator.CancelSpellChanneling();
                _spellManager.CancelSpell(_state.ChannelingElapsed);
                _state.Channeling = false;
                channeling = false;
                yield break;
            }

            yield return new WaitForSeconds(dt);
            _state.ChannelingElapsed += dt;
        }

        if (spell.isCharging) {
            _playerAnimator.CancelSpellChanneling();
            _spellManager.CancelSpell(_state.ChannelingElapsed);
        }

        _state.Channeling = false;
        channeling = false;
    }

    private IEnumerator SpellEcho(SpellData spell) {
        yield return new WaitForSeconds(0.05f);

        _recognition.ShutUp();
        EnterCastWaiting(spell);
        _spellManager.PrepareSpell(spell);
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        if (IsOwner && _meshController != null)
            _meshController.OnCast -= OnSpellCasted;
        if (_mouth != null)
            _mouth.OnMouthClose -= OnMouthClose;
    }

    private void HandleSpellKeys() {
        var index = _input.GetSpellIndexPressedThisFrame();
        if (index < 0 || index >= _recognition.Spells.Count)
            return;

        var spell = _recognition.Spells[index];
        _recognition.EmulateRecognitionFromSpell(spell, SpeechToTextHolder.Instance.Language, OnMouthClose);
    }

    private bool DisableWhileCarrying(SpellData spell) {
        return spell != null && spell.disableWhileCarrying &&
               CTFFlag.All.Any(f => f.IsCarriedBy(OwnerClientId));
    }

    private void OnMouthClose(string[] lastWords) {
        if (IsPrimalManaLocked()) return;
        if (_state.CastWaiting || _state.Channeling) return;

        var result = _recognition.Recognize(lastWords);
        _state.RecognizedSpell = result.spell;

        if (result.similarity < GameConfig.Instance.recognitionThreshold)
            return;

        if (DisableWhileCarrying(_state.RecognizedSpell)) {
            disabledSound?.Play();
            _state.RecognizedSpell = null;
            return;
        }

        StartSpellRecognition(result.spell);
    }

    private void StartSpellRecognition(SpellData spell) {
        _state.EchoCount = spell.echoCount;
        if (_state.EchoCount > 0)
            _state.SpellEcho = _state.RecognizedSpell;

        _recognition.ShutUp();

        if (spell.isCharging) {
            TryCastChargingSpell();
        } else {
            EnterCastWaiting(spell);
        }

        _spellManager.PrepareSpell(spell);
    }

    private void EnterCastWaiting(SpellData spell) {
        _state.CastWaiting = true;
        _playerAnimator.CastWaitingAnim(true, spell.castWaitingIndex);
    }

    private void TryCastChargingSpell() {
        if (IsPrimalManaLocked())
            return;

        var spellToCast = _state.SpellToCast();
        if (!_manaController.CanSpendForCast(spellToCast, _state.EchoCount))
            AddPrimalManaServerRpc(PrimalManaMissing(_manaController.CostForCast(spellToCast)));

        CastSpell();
    }

    private void HandleSpellCasting() {
        var castCharging = _input.CastPressedThisFrame() && _state.SpellToCast()?.isCharging == true;
        if (_input.CancelPressedThisFrame() || castCharging) {
            HandleCancel();
            return;
        }

        if (IsPrimalManaLocked())
            return;

        if (_state.Channeling || !_state.CastWaiting || !_input.CastPressedThisFrame())
            return;

        var spellToCast = _state.SpellToCast();
        if (!_manaController.CanSpendForCast(spellToCast, _state.EchoCount))
            AddPrimalManaServerRpc(PrimalManaMissing(_manaController.CostForCast(spellToCast)));

        _playerAnimator.CastWaitingAnim(false);
        _state.CastWaiting = false;

        if (DisableWhileCarrying(_state.RecognizedSpell)) {
            disabledSound?.Play();
            CancelSpell();
        } else {
            CastSpell();
        }

        _state.RecognizedSpell = null;
    }

    private void HandleCancel() {
        if (_state.CastWaiting) {
            _playerAnimator.CastWaitingAnim(false);
            _state.CastWaiting = false;
            _spellManager.CancelSpell(_state.ChannelingElapsed);

            _state.RecognizedSpell = null;
            _state.SpellEcho = null;
            return;
        }

        if (!_state.Channeling) return;

        if (_channelingCoroutine != null)
            StopCoroutine(_channelingCoroutine);
        _playerAnimator.CancelSpellChanneling();
        _spellManager.CancelSpell(_state.ChannelingElapsed);
        _state.SpellEcho = null;
        _state.Channeling = false;
        channeling = false;
    }

    private float PrimalManaMissing(float cost) {
        return Mathf.Max(0f, cost - mana.Value);
    }

    [ServerRpc]
    private void AddPrimalManaServerRpc(float amount) {
        primalMana.Value += amount;
    }

    [ServerRpc]
    private void SpendManaServerRpc(float amount) {
        mana.Value -= amount;
    }
}