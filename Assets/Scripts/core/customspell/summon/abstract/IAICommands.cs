using UnityEngine;

public interface IAICommands {
    void MoveTo(Vector3 pos);
    void Attack(ITarget target);
    void Idle();
}