using System;

[Flags]
public enum Preview {
    None = 0,
    Mesh = 1 << 0,
    Sphere = 1 << 1,
    Line = 1 << 2,
    Disk = 1 << 3,
}