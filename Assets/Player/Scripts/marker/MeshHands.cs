using System;
using UnityEngine;

public class MeshHands : MonoBehaviour {
    [SerializeField] private Transform invocation;
    [SerializeField] private Transform head;

    public void Bind() {
        GetComponentInParent<Player>().meshController.invocation = invocation;
        // GetComponentInParent<Player>().meshController.head = head;
    }
}