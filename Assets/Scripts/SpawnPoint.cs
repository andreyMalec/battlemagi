using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnPoint : MonoBehaviour {
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    public Transform Get() {
        var active = spawnPoints.Where(it => it.gameObject.activeSelf).ToArray();
        var random = Random.Range(0, active.Length);
        Debug.Log($"[SpawnPoint] Get Random in [0, {active.Length}] = {random}");
        return active[random];
    }

    void OnDrawGizmos() {
        foreach (Transform child in spawnPoints) {
            if (!child.gameObject.activeSelf) continue;
            Gizmos.DrawWireSphere(child.position, 0.2f);
            Gizmos.DrawLine(child.position, child.position + child.forward);
        }
    }
}