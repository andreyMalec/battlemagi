using UnityEngine;

public class GroundPointPreview : ISpellSpawnPreview {
    private Color _fillColor = new Color(0f, 1f, 1f, 0.2f);
    private float _thickness = 0.03f;
    private float _crossSize = 0.25f;

    private ISpellSpawn _spawnMode;
    private bool _active;

    private Mesh _cylinder;
    private Mesh _cube;
    private Material _mat;

    public GroundPointPreview() {
        var fillShader = Shader.Find("Custom/PreviewTransparent");
        _mat = new Material(fillShader);
        _mat.hideFlags = HideFlags.HideAndDontSave;
        _mat.color = _fillColor;
        _cylinder = BuildCylinderMesh();
        _cube = BuildPrimitiveMesh(PrimitiveType.Cube);
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

        if (context.spawn.spawnMode is not SpawnMode.GroundPoint
            && context.spawn.spawnMode is not SpawnMode.GroundPointArc
            && context.spawn.spawnMode is not SpawnMode.GroundPointArcDown
            && context.spawn.spawnMode is not SpawnMode.GroundPointForward
            && context.spawn.spawnMode is not SpawnMode.RayCast)
            return;

        foreach (var ctx in _spawnMode.ShapeCenter(context)) {
            Draw(ctx);
            break;
        }
    }

    private void Draw(SpawnContext context) {
        var pos = context.position;
        var zone = context.spell.zone;
        if (zone != null && zone.shapeType is ZoneShapeType.Plate) {
            DrawPlate(pos, context.rotation, context.spell.scale);
        }
        else {
            var up = context.rotation * Vector3.up;
            DrawDisk(pos, up, context.spell.scale);
        }

        DrawCross(pos, context.rotation);
    }

    private void DrawPlate(Vector3 pos, Quaternion rotation, float scale) {
        var side = scale * 2f;
        var height = side * 0.1f;

        RuntimeDrawFeature.Enqueue((UnityEngine.Rendering.RasterCommandBuffer cmd) => {
            var matrix = Matrix4x4.TRS(pos, rotation, new Vector3(side, height, side));
            cmd.DrawMesh(_cube, matrix, _mat, 0, 0);
        });
    }

    private void DrawDisk(Vector3 pos, Vector3 normal, float radius) {
        var rot = Quaternion.FromToRotation(Vector3.up, normal);
        var scale = new Vector3(radius * 2f, 0.02f, radius * 2f);

        RuntimeDrawFeature.Enqueue((UnityEngine.Rendering.RasterCommandBuffer cmd) => {
            var matrix = Matrix4x4.TRS(pos, rot, scale);
            cmd.DrawMesh(_cylinder, matrix, _mat, 0, 0);
        });
    }

    private void DrawCross(Vector3 pos, Quaternion rotation) {
        var right = rotation * Vector3.right;
        var forward = rotation * Vector3.forward;

        DrawLine(pos - right * _crossSize, pos + right * _crossSize);
        DrawLine(pos - forward * _crossSize, pos + forward * _crossSize);
    }

    private void DrawLine(Vector3 start, Vector3 end) {
        var dir = end - start;
        var len = dir.magnitude;
        if (len <= 0.0001f)
            return;

        var rot = Quaternion.FromToRotation(Vector3.up, dir);
        var pos = (start + end) * 0.5f;
        var scale = new Vector3(_thickness, len * 0.5f, _thickness);

        RuntimeDrawFeature.Enqueue((UnityEngine.Rendering.RasterCommandBuffer cmd) => {
            var matrix = Matrix4x4.TRS(pos, rot, scale);
            cmd.DrawMesh(_cylinder, matrix, _mat, 0, 0);
        });
    }

    private static Mesh BuildCylinderMesh() {
        return BuildPrimitiveMesh(PrimitiveType.Cylinder);
    }

    private static Mesh BuildPrimitiveMesh(PrimitiveType primitiveType) {
        var go = GameObject.CreatePrimitive(primitiveType);
        var mesh = go.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(go);
        return mesh;
    }
}