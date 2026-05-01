using System;

public enum DamageKind {
    Default,
    Fire,
    Ice,
    Lightning,
    Poison,
    Lava,
    Air,
    Magic,
    Reflect,
    Fall,
    Dirt,
}

[Flags]
public enum DamageableState {
    None = 0,
    Spawned = 1 << 0,
    Alive = 1 << 1,
    Dead = 1 << 2,
    Invulnerable = 1 << 3,
    Immortal = 1 << 4,
}

