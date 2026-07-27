using System;
using UnityEngine;

public enum Mode {
    Line,
    Tube
}

public class TrajectoryRenderer : MonoBehaviour {
    [SerializeField] private Mode mode = Mode.Line;
    [SerializeField] private Material material;
    [SerializeField] private TubeRenderer tubeRenderer;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Vector3[] points;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private void Awake() {
        if (tubeRenderer == null && mode == Mode.Tube) {
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = material;
            tubeRenderer = gameObject.AddComponent<TubeRenderer>();
        }

        if (lineRenderer == null && mode == Mode.Line) {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = material;
        }

        tubeRenderer.enabled = mode == Mode.Tube;
        lineRenderer.enabled = mode == Mode.Line;
    }

    public void Clear() {
        if (tubeRenderer != null) {
            tubeRenderer.Clear();
        }

        if (lineRenderer != null) {
            lineRenderer.positionCount = 0;
        }
    }

    public void Build(Vector3[] points) {
        this.points = points;
        if (mode == Mode.Line) {
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
        } else {
            tubeRenderer.Build(points);
        }
    }
}