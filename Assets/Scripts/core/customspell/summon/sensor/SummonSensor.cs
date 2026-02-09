using System;

[Flags]
public enum SummonSensor {
    None = 0,
    Radius = 1 << 0,
    LineOfSight = 1 << 1,
}