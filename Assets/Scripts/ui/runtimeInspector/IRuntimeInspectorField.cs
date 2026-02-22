using System;

public interface IRuntimeInspectorField<T> {
    void Bind(string label, T value, Action<T> set);
}

public interface IRuntimeInspectorEnumField {
    void Bind(string label, Type enumType, Enum value, Action<Enum> set);
}

public interface IRuntimeInspectorHeader {
    void SetTitle(string title);
}
