using System;

[Flags]
public enum SummonSensor {
    None = 0,

    /** Получает цели в радиусе */
    Radius = 1 << 0,

    /** Фильтрует цели по видимости */
    LineOfSight = 1 << 1,
}