using System;
using System.Collections.Generic;

public static class DI {
    private static readonly Dictionary<Type, object> _services = new();

    public static void Register<T>(T service) {
        var type = typeof(T);
        _services[type] = service;
    }

    public static T Get<T>() {
        var type = typeof(T);

        if (_services.TryGetValue(type, out var service))
            return (T)service;

        throw new Exception($"Service not found: {type}");
    }

    public static bool TryGet<T>(out T service) {
        if (_services.TryGetValue(typeof(T), out var obj)) {
            service = (T)obj;
            return true;
        }

        service = default;
        return false;
    }

    public static void Clear() {
        _services.Clear();
    }
}