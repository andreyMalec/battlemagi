using System;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour {
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner) {
            GetComponentInChildren<Camera>().depth = 100;
            // GetComponent<NetworkTransform>().Interpolate = false;
        } else {
            GetComponent<PlayerSpellCaster>().enabled = false;
            GetComponent<SpellManager>().enabled = false;
            GetComponent<ShowCursor>().enabled = false;
            GetComponent<CameraSelector>().enabled = false;
            GetComponentInChildren<GroundCheck>().gameObject.SetActive(false);
            GetComponentInChildren<Mouth>().gameObject.SetActive(false);
            GetComponentInChildren<Camera>().gameObject.SetActive(false);
            // GetComponent<NetworkTransform>().Interpolate = true;
        }
    }

    public void LocalRotate(Quaternion quaternion) {
        if (IsOwner) {
            transform.localRotation = quaternion;
            LocalRotateServerRpc(quaternion);
        }
    }

    public void LinearVelocity(Vector3 velocity) {
        if (IsOwner) {
            // rb.linearVelocity = velocity;
            LinearVelocityServerRpc(velocity);
        }
    }

    public void LinearVelocityPlus(Vector3 velocity) {
        if (IsOwner) {
            // rb.linearVelocity += velocity;
            LinearVelocityPlusServerRpc(velocity);
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
    private void LocalRotateServerRpc(Quaternion quaternion) {
        transform.localRotation = quaternion;
    }

    [ServerRpc]
    private void LinearVelocityServerRpc(Vector3 velocity) {
        rb.linearVelocity = velocity;
    }

    [ServerRpc]
    private void LinearVelocityPlusServerRpc(Vector3 velocity) {
        rb.linearVelocity += velocity;
    }


    [ServerRpc]
    private void AnimateBoolServerRpc(int key, bool value) {
        animator.SetBool(key, value);
    }

    [ServerRpc]
    private void AnimateFloatServerRpc(int key, float value) {
        animator.SetFloat(key, value);
    }
}