using UnityEngine;
using System;
using System.Collections.Generic;

public class RuntimeDrawHelper : MonoBehaviour {
    private static readonly List<Action> drawQueue = new();

    public static void Enqueue(Action action) {
        drawQueue.Add(action);
    }

    private void OnRenderObject() {
        foreach (var draw in drawQueue) draw?.Invoke();
        drawQueue.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists() {
        if (FindObjectOfType<RuntimeDrawHelper>() == null) {
            var go = new GameObject("[RuntimeDrawHelper]");
            go.AddComponent<RuntimeDrawHelper>();
            GameObject.DontDestroyOnLoad(go);
        }
    }
}