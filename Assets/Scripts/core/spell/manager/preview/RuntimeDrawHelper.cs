using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class RuntimeDrawHelper : MonoBehaviour {
    private static readonly List<Action> DrawQueue = new();

    public static void Enqueue(Action action) {
        DrawQueue.Add(action);
    }

    private void OnEnable() {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable() {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private static void OnEndCameraRendering(ScriptableRenderContext _, Camera cam) {
        if (cam == null)
            return;

        if (cam.cameraType != CameraType.Game)
            return;

        if (cam.targetTexture != null)
            return;

        for (var i = 0; i < DrawQueue.Count; i++) {
            DrawQueue[i]?.Invoke();
        }

        DrawQueue.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists() {
        if (FindFirstObjectByType<RuntimeDrawHelper>() == null) {
            var go = new GameObject("[RuntimeDrawHelper]");
            go.AddComponent<RuntimeDrawHelper>();
            GameObject.DontDestroyOnLoad(go);
        }
    }
}