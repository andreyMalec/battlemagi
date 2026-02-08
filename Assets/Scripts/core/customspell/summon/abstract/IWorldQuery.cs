using System.Collections.Generic;
using UnityEngine;

public interface IWorldQuery {
    ITarget FindClosestEnemy(Vector3 pos, float radius);
    bool HasLineOfSight(Vector3 from, Vector3 to);
    IEnumerable<ITarget> FindEnemiesInRadius(Vector3 pos, float radius);
}