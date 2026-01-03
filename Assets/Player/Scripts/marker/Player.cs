using System;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Reflection; // reflection for Awake

[DefaultExecutionOrder(-100)]
public class Player : NetworkBehaviour {
    private static readonly int OutlineColor = Shader.PropertyToID("OutlineColor");
    private static readonly int OutlineAlpha = Shader.PropertyToID("OutlineAlpha");

    [SerializeField] private bool isDummy = false;
    [SerializeField] private Behaviour[] scriptsToDisable;
    [SerializeField] private GameObject[] objectsToDisable;
    [SerializeField] private Camera mainCamera;
    private MeshController meshController;
    private Animator animator;

    private MethodInfo _networkAnimatorAwake;

    private void Awake() {
        // invoke Awake via reflection to rebuild internal state
        _networkAnimatorAwake = typeof(NetworkAnimator).GetMethod("Awake",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    private void SpawnAvatar(int arch) {
        var archetype = ArchetypeDatabase.Instance.GetArchetype(arch);
        var currentAvatar = Instantiate(archetype.avatarPrefab, transform); //TODO crash
        meshController = currentAvatar.GetComponent<MeshController>();
        animator = currentAvatar.GetComponent<Animator>();
        var netAnim = GetComponent<NetworkAnimator>();
        netAnim.Animator = animator;
        _networkAnimatorAwake.Invoke(netAnim, null);

        // Bind avatar to dependent components on player
        var pa = GetComponent<PlayerAnimator>();
        if (pa != null) {
            pa.animator = animator;
            pa.networkAnimator = netAnim;
            pa.meshController = meshController;
        }

        if (isDummy)
            return;

        var movement = GetComponent<FirstPersonMovement>();
        movement.movementSpeed = archetype.movementSpeed;
        movement.runSpeed = archetype.runSpeed;
        movement.maxStamina = archetype.maxStamina;
        movement.jumpStrength = archetype.jumpStrength;

        var look = GetComponent<FirstPersonLook>();
        look.BindAvatar(meshController);
        look.SetCameraOffset(archetype.cameraOffset);

        var spellMgr = GetComponent<SpellManager>();
        spellMgr.BindAvatar(meshController);
        var activeSpell = GetComponent<ActiveSpell>();
        activeSpell.BindAvatar(meshController);
        var caster = GetComponent<PlayerSpellCaster>();
        caster.maxMana = archetype.maxMana;
        caster.manaRestore = archetype.manaRegen;
        caster.BindAvatar(meshController);
        var damageable = GetComponent<Damageable>();
        damageable.maxHealth = archetype.maxHealth;
        damageable.hpRestore = archetype.healthRegen;
        var camSel = GetComponent<CameraSelector>();
        camSel.BindAvatar(meshController);
        var fpss = GetComponentInChildren<FirstPersonSounds>();
        fpss.BindAvatar(animator);
        var freeze = GetComponentInChildren<Freeze>(true);
        var footIK = currentAvatar.GetComponent<FootControllerIK>();
        freeze.BindAvatar(animator, footIK);
        var cam = GetComponentInChildren<FpsCameraClip>(true);
        cam.head = meshController.head;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        var clientId = OwnerClientId;
        Debug.Log($" [Player] OnNetworkPreSpawn called on Player_{clientId}");
        var arch = PlayerManager.Instance.FindByClientId(clientId)!.Value.Archetype;

        SpawnAvatar(arch);

        ApplyMaterial(clientId, arch);

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

            if (!IsOwner && meshController != null) {
                meshController.leftHand.weight = 0f;
                meshController.spine.weight *= 3f;
                meshController.invocation.localRotation =
                    Quaternion.Euler(new Vector3(320.634674f, 355.449707f, 39.6077499f));
            }

            mainCamera.GetComponent<Camera>().enabled = false;
        }
    }

    [ClientRpc]
    public void ApplyEffectColorClientRpc(Color color) {
        ApplyColor(prev => prev + color);
    }

    [ClientRpc]
    public void RemoveEffectColorClientRpc(Color color) {
        ApplyColor(prev => prev - color);
    }

    private void ApplyColor(Func<Color, Color> operation) {
        var materials = GetComponentInChildren<MeshBody>().GetComponent<SkinnedMeshRenderer>().materials;
        foreach (var material in materials) {
            if (!material.HasColor(OutlineColor)) continue;
            var prev = material.GetColor(OutlineColor);
            var next = operation.Invoke(prev);
            var alpha = 0f;
            if (next.a > 0)
                alpha = 1f;
            material.SetFloat(OutlineAlpha, alpha);
            material.SetColor(OutlineColor, next);
        }
    }

    public void Init(ulong clientId, Vector3 position, Quaternion rotation) {
        var arch = PlayerManager.Instance.FindByClientId(clientId)!.Value.Archetype;
        var archetype = ArchetypeDatabase.Instance.GetArchetype(arch);

        var movement = GetComponent<FirstPersonMovement>();
        movement.spawnPoint.Value = position;
        Debug.Log($"[PlayerSpawner] Init Сервер: Player_{clientId} создан в {position}, {rotation}");
        movement.stamina.Value = archetype.maxStamina;

        var damageable = GetComponent<Damageable>();
        damageable.health.Value = archetype.maxHealth;

        var caster = GetComponent<PlayerSpellCaster>();
        caster.mana.Value = archetype.maxMana;

        InitClientRpc(clientId, rotation);
    }

    [ClientRpc]
    private void InitClientRpc(ulong clientId, Quaternion rotation) {
        Debug.Log($" [PlayerSpawner] InitClientRpc Клиент: Инициализация Player_{clientId}");
        GetComponent<FirstPersonLook>().ApplyInitialRotation(rotation);
    }

    private void ApplyMaterial(ulong clientId, int arch) {
        var steamId = PlayerManager.Instance.GetSteamId(clientId);
        if (!steamId.HasValue) return;
        var archetype = ArchetypeDatabase.Instance.GetArchetype(arch);
        var color = new Friend(steamId.Value).GetColor();
        var bodyMat = new Material(archetype.bodyShader);
        bodyMat.SetFloat(ColorizeMesh.Hue, color.hue);
        bodyMat.SetFloat(ColorizeMesh.Saturation, color.saturation);
        GetComponentInChildren<MeshBody>().gameObject.GetComponent<SkinnedMeshRenderer>().material = bodyMat;
        if (archetype.cloakShader == null) return;
        var cloakMat = new Material(archetype.cloakShader);
        cloakMat.SetFloat(ColorizeMesh.Hue, color.hue);
        cloakMat.SetFloat(ColorizeMesh.Saturation, color.saturation);
        var meshCloak = GetComponentInChildren<MeshCloak>();
        if (meshCloak != null)
            meshCloak.gameObject.GetComponent<SkinnedMeshRenderer>().material = cloakMat;
    }
}