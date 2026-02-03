using System;

[Flags]
public enum HitOutcome {
    None = 0,

    /** исчезает */
    Destroy = 1 << 0,

    /** отражается */
    Bounce = 1 << 1,

    /** проходит сквозь */
    Pierce = 1 << 2,

    /** порождает новые */
    Fork = 1 << 3
}