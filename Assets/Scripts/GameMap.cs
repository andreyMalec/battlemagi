using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Map", menuName = "Game/Map")]
public class GameMap : ScriptableObject {
    public string mapName;
    public SceneAsset scene;
}