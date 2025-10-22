using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnPoint : MonoBehaviour {
    public TeamManager.Team team;

    void OnDrawGizmos() {
        if (team == TeamManager.Team.Blue)
            Gizmos.color = Color.blue;
        if (team == TeamManager.Team.Red)
            Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}