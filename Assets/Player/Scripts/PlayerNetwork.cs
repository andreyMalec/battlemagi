using System;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerNetwork : NetworkBehaviour {
    [SerializeField] private Animator animator;
    [SerializeField] private Behaviour[] scriptsToDisable;
    [SerializeField] private GameObject[] objectsToDisable;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Rig hand;
    [SerializeField] private Rig spine;
    
    public NetworkVariable<ulong> steamId = new NetworkVariable<ulong>();

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner) {
            mainCamera.GetComponent<Camera>().depth = 100;
            SetSteamIDServerRpc(SteamClient.SteamId.Value);
        } else {
            foreach (var script in scriptsToDisable) {
                script.enabled = false;
            }

            foreach (var obj in objectsToDisable) {
                obj.SetActive(false);
            }

            hand.weight = 0f;
            spine.weight *= 3f;
            mainCamera.GetComponent<Camera>().enabled = false;
        }

        Debug.Log($"OnNetworkSpawn id={steamId.Value}");
        LobbyHolder.instance.players[steamId.Value] = gameObject;
    }
    
    [ServerRpc]
    private void SetSteamIDServerRpc(ulong _steamId)
    {
        steamId.Value = _steamId;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        LobbyHolder.instance.players.Remove(steamId.Value);
    }

    public void AnimateBool(int key, bool value) {
        if (IsOwner) {
            animator.SetBool(key, value);
            AnimateBoolServerRpc(key, value);
        }
    }

    public void AnimateFloat(int key, float value) {
        if (IsOwner) {
            animator.SetFloat(key, value);
            AnimateFloatServerRpc(key, value);
        }
    }

//=====================================================

    [ServerRpc]
    private void AnimateBoolServerRpc(int key, bool value) {
        animator.SetBool(key, value);
    }

    [ServerRpc]
    private void AnimateFloatServerRpc(int key, float value) {
        animator.SetFloat(key, value);
    }
}