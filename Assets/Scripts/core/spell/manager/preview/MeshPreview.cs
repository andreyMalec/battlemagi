using System.Collections;
using UnityEngine;

public class MeshPreview : ISpellPreview {
    private Color fillColor = new Color(0f, 1f, 1f, 0.2f);
    private Color wireColor = Color.cyan;
    private Vector3 scale = Vector3.one;
    private Vector3 position = Vector3.zero;
    private Quaternion rotation = Quaternion.identity;

    private Mesh mesh;
    private Material fillMat;
    private Material wireMat;
    private ISpawnStrategy _spawnMode;
    private SpellData _spell;

    private bool _active;

    public MeshPreview() {
        // Полупрозрачная заливка
        fillMat = new Material(Shader.Find("Custom/UnlitTransparent"));
        fillMat.color = fillColor;

        // Проволочный контур
        wireMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        wireMat.hideFlags = HideFlags.HideAndDontSave;
        wireMat.color = fillColor;
        wireMat.SetInt("_ZWrite", 0);
        wireMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        wireMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        wireMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
    }

    public void Show(ActiveSpell manager, ISpawnStrategy spawnMode, SpellData spell) {
        _spawnMode = spawnMode;
        _spell = spell;
        var m = spell.mainSpellPrefab.GetComponentInChildren<MeshFilter>();
        mesh = m.sharedMesh;
        scale = m.gameObject.transform.localScale;
        position = m.gameObject.transform.localPosition;
        rotation = m.gameObject.transform.localRotation;
        _active = true;
    }

    public void Clear(ActiveSpell manager) {
        _active = false;
    }

    public void Update(ActiveSpell manager) {
        if (!_active) return;

        RunBlocking(_spawnMode.Spawn(manager.spellManager, _spell, Draw));
    }

    private void Draw(SpellData spell, Vector3 pos, Quaternion rot, int index) {
        RuntimeDrawHelper.Enqueue(() => {
            Matrix4x4 matrix = Matrix4x4.TRS(pos + position, rot * rotation, scale);

            // Сначала заливка
            fillMat.SetPass(0);
            Graphics.DrawMeshNow(mesh, matrix);

            // Затем контур
            GL.wireframe = true;
            wireMat.SetPass(0);
            GL.Color(wireColor);
            Graphics.DrawMeshNow(mesh, matrix);
            GL.wireframe = false;
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
}