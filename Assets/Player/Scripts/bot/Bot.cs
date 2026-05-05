using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Reflection; // reflection for Awake

[DefaultExecutionOrder(-100)]
public class Bot : NetworkBehaviour {
    private static readonly int OutlineColor = Shader.PropertyToID("OutlineColor");
    private static readonly int OutlineAlpha = Shader.PropertyToID("OutlineAlpha");

    private MeshController meshController;
    private Animator animator;
    private bool _avatarSpawned;

    public readonly NetworkVariable<ulong> BotIdValue = new();
    public readonly NetworkVariable<int> ArchetypeValue = new();
    public readonly NetworkVariable<float> HueValue = new();
    public readonly NetworkVariable<float> SaturationValue = new();

    private MethodInfo _networkAnimatorAwake;

    public int ArchetypeId => ArchetypeValue.Value;
    public ulong BotId => BotIdValue.Value;

    private void Awake() {
        // invoke Awake via reflection to rebuild internal state
        _networkAnimatorAwake = typeof(NetworkAnimator).GetMethod("Awake",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        var arch = ArchetypeValue.Value;
        Debug.Log($" [Bot] OnNetworkSpawn called on Bot_{BotId} with archetype {arch}");

        SpawnAvatar(arch);

        ApplyMaterial(arch, HueValue.Value, SaturationValue.Value);

        gameObject.name = $"Bot_{BotId}";

        if (IsOwner) {
        } else {
            if (!IsOwner && meshController != null) {
                meshController.leftHand.weight = 0f;
                meshController.spine.weight *= 3f;
                meshController.invocation.localRotation =
                    Quaternion.Euler(new Vector3(320.634674f, 355.449707f, 39.6077499f));
            }
        }
    }

    private void SpawnAvatar(int arch) {
        if (_avatarSpawned)
            return;

        var archetype = ArchetypeDatabase.Instance.GetArchetype(arch);
        var currentAvatar = Instantiate(archetype.avatarPrefab, transform); //TODO crash
        _avatarSpawned = true;
        meshController = currentAvatar.GetComponent<MeshController>();
        animator = currentAvatar.GetComponent<Animator>();
        var netAnim = GetComponent<NetworkAnimator>();
        netAnim.Animator = animator;
        _networkAnimatorAwake.Invoke(netAnim, null);

        // Bind avatar to dependent components on player
        var ba = GetComponent<BotAnimator>();
        if (ba != null) {
            ba.animator = animator;
            ba.networkAnimator = netAnim;
            ba.meshController = meshController;
        }

        var scpa = GetComponent<SpellCasterPlayerAnimator>();
        scpa?.BindAvatar(meshController, netAnim, animator, IsOwner);
        var scpp = GetComponent<SpellCasterPlayerPreview>();
        scpp?.BindAvatar(meshController);

        var movement = GetComponent<BotMovement>();
        movement.movementSpeed = archetype.movementSpeed;
        movement.maxStamina = archetype.maxStamina;
        movement.jumpStrength = archetype.jumpStrength;

        var caster = GetComponent<SpellCasterPlayer>();
        caster.Mana.SetDefaults(archetype.maxMana, archetype.manaRegen);
        var damageable = GetComponent<Damageable>();
        damageable.Health.SetDefaults(archetype.maxHealth, archetype.healthRegen);
        var fpss = GetComponentInChildren<FirstPersonSounds>();
        fpss.BindAvatar(animator);
        var freeze = GetComponentInChildren<Freeze>(true);
        var footIK = currentAvatar.GetComponent<FootControllerIK>();
        freeze.BindAvatar(animator, footIK);
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

    public void ApplyPlayerState(ulong botId, int archetype, float hue, float saturation) {
        BotIdValue.Value = botId;
        ArchetypeValue.Value = archetype;
        HueValue.Value = hue;
        SaturationValue.Value = saturation;
    }

    private void ApplyMaterial(int arch, float hue, float saturation) {
        var archetype = ArchetypeDatabase.Instance.GetArchetype(arch);
        var bodyMat = new Material(archetype.bodyShader);
        bodyMat.SetFloat(ColorizeMesh.Hue, hue);
        bodyMat.SetFloat(ColorizeMesh.Saturation, saturation);
        bodyMat.SetFloat(ColorizeMesh.Value, ColorizeMesh.CalculateValue());
        GetComponentInChildren<MeshBody>().gameObject.GetComponent<SkinnedMeshRenderer>().material = bodyMat;
        if (archetype.cloakShader == null) return;
        var cloakMat = new Material(archetype.cloakShader);
        cloakMat.SetFloat(ColorizeMesh.Hue, hue);
        cloakMat.SetFloat(ColorizeMesh.Saturation, saturation);
        var meshCloak = GetComponentInChildren<MeshCloak>();
        if (meshCloak != null)
            meshCloak.gameObject.GetComponent<SkinnedMeshRenderer>().material = cloakMat;
    }
}