using UnityEngine;
using System.Text;

public class PerformanceOverlay : MonoBehaviour {
    [Header("FPS")]
    public float updateInterval = 0.5f;

    [Header("Display")]
    public bool showMemory = true;

    public bool showFrameTime = true;
    public bool showGC = true;

    float timeLeft;
    int frames;
    float fps;

    long monoUsed;
    long totalAllocated;

    StringBuilder sb = new StringBuilder(256);

    void Start() {
        timeLeft = updateInterval;
    }

    void Update() {
        timeLeft -= Time.unscaledDeltaTime;
        frames++;

        if (timeLeft <= 0f) {
            fps = frames / updateInterval;
            frames = 0;
            timeLeft = updateInterval;

            if (showMemory) {
                monoUsed = System.GC.GetTotalMemory(false);
                totalAllocated = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            }
        }
    }

    void OnGUI() {
        sb.Clear();

        sb.AppendLine($"FPS: {fps:F1}");

        if (showFrameTime)
            sb.AppendLine($"Frame: {(1000f / Mathf.Max(fps, 0.01f)):F2} ms");

        if (showMemory) {
            sb.AppendLine($"Mono: {monoUsed / (1024 * 1024)} MB");
            sb.AppendLine($"Total: {totalAllocated / (1024 * 1024)} MB");
        }

        if (showGC) {
            sb.AppendLine($"GC Alloc/frame: {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024 * 1024)} MB");
        }

        GUI.color = Color.white;
        GUI.Label(new Rect(10, 10, 300, 200), sb.ToString());
    }
}