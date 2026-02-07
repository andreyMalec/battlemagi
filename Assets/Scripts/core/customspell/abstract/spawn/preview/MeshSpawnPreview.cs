using System.Collections;
using UnityEngine;

public class MeshSpawnPreview : ISpellSpawnPreview {
    private static readonly int Thickness = Shader.PropertyToID("_Thickness");
    private Color fillColor = new Color(0f, 1f, 1f, 0.2f);
    private Color wireColor = Color.cyan;
    private Vector3 scale = Vector3.one;
    private Vector3 position = Vector3.zero;
    private Quaternion rotation = Quaternion.identity;

    private Mesh mesh;
    private Material fillMat;
    private Material wireMat;
    private SpellDefinition _spell;
    private ISpellSpawn _spawnMode;

    private bool _active;

    private Mesh _wireMesh;

    public MeshSpawnPreview() {
        var fillShader = Shader.Find("Custom/PreviewTransparent");
        fillMat = new Material(fillShader);
        fillMat.hideFlags = HideFlags.HideAndDontSave;
        fillMat.color = fillColor;

        var wireShader = Shader.Find("Custom/PreviewWireframe");
        wireMat = new Material(wireShader);
        wireMat.hideFlags = HideFlags.HideAndDontSave;
        wireMat.color = wireColor;
        wireMat.SetFloat(Thickness, 1.3f);
    }

    public void Show(SpawnContext context, ISpellSpawn spawnMode) {
        _spawnMode = spawnMode;
        _spell = context.spell;
        var prefab = SpellPrefabDatabase.Instance.Get(_spell.prefabId);
        var m = prefab.GetComponentInChildren<MeshFilter>(true);
        mesh = m.sharedMesh;
        _wireMesh = BuildWireMesh(mesh);
        scale = m.gameObject.transform.localScale * context.spell.zoneRadius;
        position = m.gameObject.transform.localPosition;
        rotation = m.gameObject.transform.localRotation;
        _active = true;
    }

    public void Clear() {
        _active = false;
    }

    public void Update(SpawnContext context) {
        if (!_active) return;

        RunBlocking(_spawnMode.Request(context, Draw));
    }

    private void Draw(SpawnContext context, int index) {
        RuntimeDrawFeature.Enqueue((UnityEngine.Rendering.RasterCommandBuffer cmd) => {
            var matrix = Matrix4x4.TRS(context.position + position, context.rotation * rotation, scale);
            cmd.DrawMesh(mesh, matrix, fillMat, 0, 0);
            cmd.DrawMesh(_wireMesh, matrix, wireMat, 0, 0);
        });
    }

    private static void RunBlocking(IEnumerator routine) {
        while (routine.MoveNext()) {
            if (routine.Current is IEnumerator nested) {
                RunBlocking(nested); // рекурсивно пройти вложенные
            }
            // Игнорируем всё остальное (WaitForSeconds, null и т.д.)
        }
    }

    private static Mesh BuildWireMesh(Mesh source) {
        var indices = source.triangles;
        var srcVertices = source.vertices;
        var srcNormals = source.normals;
        var srcTangents = source.tangents;
        var srcUv2 = source.uv2;
        var srcColors = source.colors;

        var vertexCount = indices.Length;
        var vertices = new Vector3[vertexCount];
        var normals = (srcNormals != null && srcNormals.Length == srcVertices.Length) ? new Vector3[vertexCount] : null;
        var tangents = (srcTangents != null && srcTangents.Length == srcVertices.Length)
            ? new Vector4[vertexCount]
            : null;
        var uv = new Vector2[vertexCount];
        var uv2 = (srcUv2 != null && srcUv2.Length == srcVertices.Length) ? new Vector2[vertexCount] : null;
        var colors = (srcColors != null && srcColors.Length == srcVertices.Length) ? new Color[vertexCount] : null;

        var outIndices = new int[vertexCount];

        for (var i = 0; i < vertexCount; i += 3) {
            var i0 = indices[i + 0];
            var i1 = indices[i + 1];
            var i2 = indices[i + 2];

            vertices[i + 0] = srcVertices[i0];
            vertices[i + 1] = srcVertices[i1];
            vertices[i + 2] = srcVertices[i2];

            if (normals != null) {
                normals[i + 0] = srcNormals[i0];
                normals[i + 1] = srcNormals[i1];
                normals[i + 2] = srcNormals[i2];
            }

            if (tangents != null) {
                tangents[i + 0] = srcTangents[i0];
                tangents[i + 1] = srcTangents[i1];
                tangents[i + 2] = srcTangents[i2];
            }

            if (uv2 != null) {
                uv2[i + 0] = srcUv2[i0];
                uv2[i + 1] = srcUv2[i1];
                uv2[i + 2] = srcUv2[i2];
            }

            if (colors != null) {
                colors[i + 0] = srcColors[i0];
                colors[i + 1] = srcColors[i1];
                colors[i + 2] = srcColors[i2];
            }

            uv[i + 0] = new Vector2(1, 0);
            uv[i + 1] = new Vector2(0, 1);
            uv[i + 2] = new Vector2(0, 0);

            outIndices[i + 0] = i + 0;
            outIndices[i + 1] = i + 1;
            outIndices[i + 2] = i + 2;
        }

        var mesh = new Mesh();
        mesh.indexFormat = (vertexCount > 65535)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = vertices;
        if (normals != null) mesh.normals = normals;
        if (tangents != null) mesh.tangents = tangents;
        mesh.uv = uv;
        if (uv2 != null) mesh.uv2 = uv2;
        if (colors != null) mesh.colors = colors;

        mesh.triangles = outIndices;
        mesh.RecalculateBounds();
        return mesh;
    }
}