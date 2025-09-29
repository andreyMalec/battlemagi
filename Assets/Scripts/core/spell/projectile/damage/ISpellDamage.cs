using UnityEngine;

public interface ISpellDamage {
    void OnHit(Collider other);
    void OnStay(Collider other); // для DoT
}