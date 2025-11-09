using UnityEngine;

public interface ISpellDamage {
    bool OnEnter(Collider other);
    bool OnExit(Collider other);
    bool Update();
}