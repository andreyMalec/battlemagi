using System.Collections;
using UnityEngine;

public class DiskPreview : ISpellSpawnPreview {
    private Color _fillColor = new Color(0f, 1f, 1f, 0.2f);

    private ISpellSpawn _spawnMode;
    private bool _active;

    private Mesh _mesh;
    private Material _mat;

    public DiskPreview() {
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

        var ctx = ISpellSpawn.GroundPos(context, context.forward, out _, Vector3.down);
        Draw(ctx with {
            position = ctx.position + ctx.rotation * new Vector3(0f, context.spawn.circleHeight, 0f)
        }, 0);
    }

    private void Draw(SpawnContext context, int index) {
        var center = context.position - context.rotation * new Vector3(0f, context.spawn.circleHeight, 0f);
        var normal = context.rotation * Vector3.up;

        DrawDisk(center, normal, context.spawn.circleRadius, context.spawn.circleHeight);
    }

    private void DrawDisk(Vector3 groundPoint, Vector3 normal, float radius, float height) {
        var pos = groundPoint + normal.normalized * height;
        var scale = new Vector3(radius * 2f, 0.02f, radius * 2f);
        var rot = Quaternion.FromToRotation(Vector3.up, normal);

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
