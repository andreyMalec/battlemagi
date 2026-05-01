using UnityEngine;

public interface ILocomotion {
    void Move(AIContext ctx, Vector3 target);
    void Stop();
}