using UnityEngine;
using UnityEditor;
using System.IO;

public static class MazeTextureSaver
{
    [MenuItem("Tools/Save Maze Texture PNG")]
    public static void SaveMazeTexture()
    {
        int w = 40;
        int h = 40;
        int cellSize = 8;
        int wallThickness = 2;
        int seed = 12345; // example seed

        var cells = MazeGenerator.Generate(w, h, seed);
        Texture2D tex = MazeGenerator.RenderToTexture(cells, cellSize, wallThickness);

        byte[] bytes = tex.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, "maze_" + seed + ".png");
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(path);
        EditorUtility.DisplayDialog("Maze saved", "Saved maze PNG to:\n" + path, "OK");
    }
}
