using System.Collections.Generic;
using UnityEngine;

public interface IAICommands {
    void MoveTo(Vector3 pos);
    void Attack(AIContext ctx);
    void Idle();
    void Tick(AIContext ctx);
}