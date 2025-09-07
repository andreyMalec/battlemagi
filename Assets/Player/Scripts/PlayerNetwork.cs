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
    [SerializeField] private MeshController meshController;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner) {
            mainCamera.GetComponent<Camera>().depth = 100;
        } else {
            foreach (var script in scriptsToDisable) {
                script.enabled = false;
            }

            foreach (var obj in objectsToDisable) {
                obj.SetActive(false);
            }

            meshController.leftHand.weight = 0f;
            meshController.spine.weight *= 3f;
            mainCamera.GetComponent<Camera>().enabled = false;
        }
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