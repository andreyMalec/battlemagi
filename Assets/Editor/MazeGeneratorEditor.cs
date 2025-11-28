using System.IO;
using UnityEditor;
using UnityEngine;

public class MazeGeneratorEditor : EditorWindow
{
    int mazeWidth = 20;
    int mazeHeight = 20;
    int cellSize = 8;
    int wallThickness = 2;
    bool useSeed = false;
    int seed = 0;

    [MenuItem("Tools/Maze Generator")]
    static void OpenWindow()
    {
        GetWindow<MazeGeneratorEditor>("Maze Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Maze Settings", EditorStyles.boldLabel);
        mazeWidth = EditorGUILayout.IntField("Width (cells)", mazeWidth);
        mazeHeight = EditorGUILayout.IntField("Height (cells)", mazeHeight);
        cellSize = EditorGUILayout.IntField("Cell size (px)", cellSize);
        wallThickness = EditorGUILayout.IntField("Wall thickness (px)", wallThickness);

        useSeed = EditorGUILayout.Toggle("Use seed", useSeed);
        using (new EditorGUILayout.HorizontalScope())
        {
            seed = EditorGUILayout.IntField("Seed", seed);
            if (GUILayout.Button("Randomize", GUILayout.MaxWidth(90)))
            {
                seed = new System.Random().Next();
            }
        }

        if (GUILayout.Button("Generate and Save PNG"))
        {
            var cells = useSeed ? MazeGenerator.Generate(mazeWidth, mazeHeight, seed) : MazeGenerator.Generate(mazeWidth, mazeHeight);
            var tex = MazeGenerator.RenderToTexture(cells, cellSize, wallThickness);
            byte[] bytes = tex.EncodeToPNG();
            string defaultName = useSeed ? $"maze_{seed}.png" : "maze.png";
            string path = EditorUtility.SaveFilePanel("Save Maze PNG", Application.dataPath, defaultName, "png");
            if (!string.IsNullOrEmpty(path)) File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
        }
    }
}
