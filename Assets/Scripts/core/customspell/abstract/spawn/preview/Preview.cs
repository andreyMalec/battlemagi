using System;

[Flags]
public enum Preview {
    None = 0,
    Mesh = 1 << 0,
    Line = 1 << 1,
    Disk = 1 << 2,
    GroundPoint = 1 << 3,
}