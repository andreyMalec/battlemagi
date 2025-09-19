using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour {
    public List<Transform> spawnPoints = new List<Transform>();

    private void Awake() {
        foreach (Transform child in transform) {
            spawnPoints.Add(child.transform);
        }
    }

    void OnDrawGizmos() {
        foreach (Transform child in spawnPoints) {
            Gizmos.DrawWireSphere(child.position, 0.2f);
        }
    }
}