using UnityEngine;

public interface IProjectileDamage {
    void OnHit(Collider other);
    void OnStay(Collider other); // для DoT
}