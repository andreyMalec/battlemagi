using System.Collections;
using UnityEngine;

public class GroundRayPreview : ISpellSpawnPreview {
    private Color _fillColor = new Color(0f, 1f, 1f, 0.2f);
    private float _thickness = 0.03f;

    private ISpellSpawn _spawnMode;
    private bool _active;

    private Mesh _mesh;
    private Material _mat;

    public GroundRayPreview() {
        var fillShader = Shader.Find("Custom/PreviewTransparent");
        _mat = new Material(fillShader);
        _mat.hideFlags = HideFlags.HideAndDontSave;
        _mat.color = _fillColor;
        _mesh = BuildCylinderMesh();
    }

    public void Show(SpawnContext context, ISpellSpawn spawnMode) {
        _spawnMode = spawnMode;
        _active = true;
    }

    public void Clear() {
        _active = false;
    }

    public void Update(SpawnContext context) {
        if (!_active) return;

        if (context.spawn.spawnMode is not SpawnMode.GroundPointCircleUp
            && context.spawn.spawnMode is not SpawnMode.GroundPointDiskUp)
            return;

        var hasFirst = false;
        var hasSecond = false;
        SpawnContext first = default;
        SpawnContext second = default;

        foreach (var c in _spawnMode.ShapeCenter(context)) {
            if (!hasFirst) {
                first = c;
                hasFirst = true;
                continue;
            }

            second = c;
            hasSecond = true;
            break;
        }

        if (!hasFirst) return;

        var start = first.position;
        var end = hasSecond
            ? second.position
            : first.position + first.rotation * new Vector3(0f, context.spawn.circleHeight, 0f);

        Draw(start, end);
    }

    private void Draw(SpawnContext context, int index) {
        var groundPoint = context.position - context.rotation * new Vector3(0f, context.spawn.circleHeight, 0f);
        Draw(groundPoint, context.position);
    }

    private void Draw(Vector3 start, Vector3 end) {
        var dir = end - start;
        var len = dir.magnitude;
        if (len <= 0.0001f)
            return;

        var rot = Quaternion.FromToRotation(Vector3.up, dir);
        var pos = (start + end) * 0.5f;
        var scale = new Vector3(_thickness, len * 0.5f, _thickness);

        RuntimeDrawFeature.Enqueue((UnityEngine.Rendering.RasterCommandBuffer cmd) => {
            var matrix = Matrix4x4.TRS(pos, rot, scale);
            cmd.DrawMesh(_mesh, matrix, _mat, 0, 0);
        });
    }

    private static void RunBlocking(IEnumerator routine) {
        while (routine.MoveNext()) {
            if (routine.Current is IEnumerator nested) {
                RunBlocking(nested);
            }
        }
    }

    private static Mesh BuildCylinderMesh() {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var mesh = go.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(go);
        return mesh;
    }
}