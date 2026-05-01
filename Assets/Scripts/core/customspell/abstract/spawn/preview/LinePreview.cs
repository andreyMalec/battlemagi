using System.Collections;
using UnityEngine;

public class LinePreview : ISpellSpawnPreview {
    private Color fillColor = new Color(0f, 1f, 1f, 0.2f);
    private float _thickness = 0.01f;

    private ISpellSpawn _spawnMode;
    private bool _active;

    private Mesh _mesh;
    private Material _mat;

    public LinePreview() {
        var fillShader = Shader.Find("Custom/PreviewTransparent");
        _mat = new Material(fillShader);
        _mat.hideFlags = HideFlags.HideAndDontSave;
        _mat.color = fillColor;
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

        RunBlocking(_spawnMode.Request(context, Draw));
    }

    private void Draw(SpawnContext context) {
        var start = context.caster.transform.position;
        var end = context.position;
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