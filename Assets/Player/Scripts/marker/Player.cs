using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(-100)]
public class Player : NetworkBehaviour {
    public static Player local;

    [SerializeField] private bool isDummy = false;
    [SerializeField] private Behaviour[] scriptsToDisable;
    [SerializeField] private GameObject[] objectsToDisable;
    [SerializeField] private Camera mainCamera;
    private GameObject bodyAvatar;
    private GameObject handsAvatar;
    public MeshController meshController;
    public MeshBody meshBody;
    public MeshHands meshHands;
    private Animator animator;
    private Animator handsAnimator;
    private bool _avatarSpawned;

    public readonly NetworkVariable<ulong> SteamIdValue = new();
    public readonly NetworkVariable<int> ArchetypeValue = new();
    public readonly NetworkVariable<float> HueValue = new();
    public readonly NetworkVariable<float> SaturationValue = new();

    public int ArchetypeId => ArchetypeValue.Value;
    public ulong SteamId => SteamIdValue.Value;

    private int _cameraIndex = 0;

    private float _timeScale = 1;

    private void TimeScale() {
        var sign = Input.GetKeyDown(KeyCode.DownArrow) ? -1 : 0;
        if (sign == 0)
            sign = Input.GetKeyDown(KeyCode.UpArrow) ? 1 : 0;
        if (Input.GetKeyDown(KeyCode.Keypad0)) {
            _timeScale = 1;
            Time.timeScale = _timeScale;
        }

        if (sign != 0) {
            _timeScale += sign * 0.1f;
            _timeScale = Mathf.Clamp(_timeScale, 0.1f, 2f);
            Debug.Log($" ______________ timeScale : {_timeScale}");
            Time.timeScale = _timeScale;
        }
    }

    public void Update() {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.P)) {
            var i = 0;
            var cameras = GetComponentsInChildren<Camera>()
                .Filter(it => it.targetTexture == null).ToArray();
            _cameraIndex = (_cameraIndex + 1) % cameras.Length;
            var isFP = _cameraIndex == 0;
            foreach (var cam in cameras) {
                cam.enabled = i == _cameraIndex;
                i++;
            }

            LocalBodyAvatar(!isFP);
            BindHand();
            var cloak = meshController.cloak;
            if (cloak == null) return;
            if (isFP) {
                cloak.GetComponent<SkinnedMeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            } else {
                cloak.GetComponent<SkinnedMeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            }
        }
    }

    private void BindHand() {
        var isFP = _cameraIndex == 0;
        var scpp = GetComponent<SpellCasterPlayerPreview>();
        if (isFP && IsOwner) {
            scpp?.BindHand(meshHands.invocation);
        } else {
            scpp?.BindHand(meshController.invocation);
        }
    }

    private void SpawnAvatar(int arch) {
        if (_avatarSpawned)
            return;

        var archetype = Ctx.GetArchetype(arch);
        bodyAvatar = Instantiate(archetype.avatarPrefab, transform);
        _avatarSpawned = true;
        meshController = bodyAvatar.GetComponent<MeshController>();
        meshBody = bodyAvatar.GetComponentInChildren<MeshBody>();
        animator = bodyAvatar.GetComponent<Animator>();
        var netAnim = GetComponent<NetworkAnimator>();
        netAnim.Animator = animator;
        animator.Rebind();

        // Bind avatar to dependent components on player
        var pa = GetComponent<PlayerAnimator>();
        if (pa != null) {
            pa.animator = animator;
            pa.secondaryAnimator = null;
        }

        var scpa = GetComponent<SpellCasterPlayerAnimator>();
        if (IsOwner && archetype.avatarHandsPrefab != null) {
            SpawnHandsAvatar(archetype.avatarHandsPrefab);
            LocalBodyAvatar(false);
            if (pa != null)
                pa.secondaryAnimator = handsAnimator;
            scpa?.BindHandsAnimator(handsAnimator);
            meshBody.gameObject.layer = LayerMask.NameToLayer("Mirror");
        } else {
            scpa?.BindHandsAnimator(null);
        }

        scpa?.BindAvatar(meshController, netAnim, animator, IsOwner);
        BindHand();

        if (isDummy)
            return;

        var movement = GetComponent<FirstPersonMovement>();
        movement.movementSpeed = archetype.movementSpeed;
        movement.runSpeed = archetype.runSpeed;
        movement.jumpStrength = archetype.jumpStrength;

        var caster = GetComponent<SpellCasterPlayer>();
        if (GameModeRules.IsChargedShotOnlyMode())
            caster.Mana.SetDefaults(120, 60);
        else
            caster.Mana.SetDefaults(archetype.maxMana, archetype.manaRegen);
        var damageable = GetComponent<Damageable>();
        damageable.Health.SetDefaults(archetype.maxHealth, archetype.healthRegen);
        var passiveRuntime = GetComponent<ArchetypePassiveRuntime>();
        passiveRuntime.Configure(archetype.passive);
        var fpss = GetComponentInChildren<FirstPersonSounds>();
        fpss.BindAvatar(animator);
        var freeze = GetComponentInChildren<Freeze>(true);
        var footIK = bodyAvatar.GetComponent<FootControllerIK>();
        freeze.BindAvatar(footIK);
    }

    private void SpawnHandsAvatar(GameObject handsPrefab) {
        handsAvatar = Instantiate(handsPrefab, mainCamera.transform);
        meshHands = handsAvatar.GetComponent<MeshHands>();
        meshHands.Bind(meshController);

        handsAnimator = handsAvatar.GetComponent<Animator>();
        var bodyRuntimeController = animator.runtimeAnimatorController;
        if (handsAnimator != null && handsAnimator.runtimeAnimatorController == null)
            handsAnimator.runtimeAnimatorController = bodyRuntimeController;

        var cam = GetComponentInChildren<FpsCameraClip>(true);
        cam.BindHands(handsAvatar.transform);
    }

    public void LocalBodyAvatar(bool visible) {
        var renderers = bodyAvatar.GetComponentsInChildren<Renderer>(true);
        foreach (var avatarRenderer in renderers) {
            if (!avatarRenderer.TryGetComponent<MeshBody>(out _))
                avatarRenderer.enabled = visible;
        }

        if (handsAvatar == null) return;
        renderers = handsAvatar.GetComponentsInChildren<Renderer>(true);
        foreach (var avatarRenderer in renderers) {
            avatarRenderer.enabled = !visible;
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsOwner)
            local = this;

        var clientId = OwnerClientId;
        var arch = ArchetypeValue.Value;
        Debug.Log($" [Player] OnNetworkSpawn called on Player_{clientId} with archetype {arch}");

        SpawnAvatar(arch);

        ApplyMaterial(arch, HueValue.Value, SaturationValue.Value);

        gameObject.name = $"Player_{OwnerClientId}";

        if (isDummy)
            return;

        if (IsOwner) {
            mainCamera.GetComponent<Camera>().depth = 100;
        } else {
            foreach (var script in scriptsToDisable) {
                script.enabled = false;
            }

            foreach (var obj in objectsToDisable) {
                obj.SetActive(false);
            }

            mainCamera.GetComponent<Camera>().enabled = false;
        }
    }

    public void Init(ulong ownerId, Vector3 position, Quaternion rotation) {
        var movement = GetComponent<FirstPersonMovement>();
        movement.spawnPoint.Value = position;
        Debug.Log($"[PlayerSpawner] Init Сервер: Player_{ownerId} создан в {position}, {rotation}");

        InitClientRpc(ownerId, rotation);
    }

    public void ApplyPlayerState(ulong steamId, int archetype, float hue, float saturation) {
        SteamIdValue.Value = steamId;
        ArchetypeValue.Value = archetype;
        HueValue.Value = hue;
        SaturationValue.Value = saturation;
    }

    [ClientRpc]
    private void InitClientRpc(ulong ownerId, Quaternion rotation) {
        Debug.Log($" [PlayerSpawner] InitClientRpc Клиент: Инициализация Player_{ownerId}");
        GetComponent<FirstPersonLook>().ApplyInitialRotation(rotation);
        var participantIdentity = GetComponent<ParticipantIdentity>();
        participantIdentity.SetParticipantId(ParticipantIdentityCodec.Decode(ownerId));
        foreach (var identityUser in GetComponents<IdentityUser>()) {
            identityUser.Use(gameObject);
        }
    }

    private void ApplyMaterial(int arch, float hue, float saturation) {
        var archetype = Ctx.GetArchetype(arch);
        var bodyMat = new Material(archetype.bodyShader);
        bodyMat.SetFloat(ColorizeMesh.Hue, hue);
        bodyMat.SetFloat(ColorizeMesh.Saturation, saturation);
        bodyMat.SetFloat(ColorizeMesh.Value, ColorizeMesh.CalculateValue());
        meshBody.GetComponent<SkinnedMeshRenderer>().material = bodyMat;
        if (handsAvatar != null)
            handsAvatar.GetComponentInChildren<SkinnedMeshRenderer>().material = bodyMat;
        if (archetype.cloakShader == null) return;
        var cloakMat = new Material(archetype.cloakShader);
        cloakMat.SetFloat(ColorizeMesh.Hue, hue);
        cloakMat.SetFloat(ColorizeMesh.Saturation, saturation);
        var meshCloak = meshController.GetComponentInChildren<MeshCloak>();
        if (meshCloak != null)
            meshCloak.gameObject.GetComponent<SkinnedMeshRenderer>().material = cloakMat;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        if (IsOwner && local == this) {
            local = null;
        }
    }
}