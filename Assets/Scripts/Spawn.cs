using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class Spawn : MonoBehaviour {
    private readonly List<SpawnPoint> _spawnPoints = new();

    private void Awake() {
        RefreshSpawnPoints(false);
    }

#if UNITY_EDITOR
    private void OnEnable() {
        // Слушаем любые изменения иерархии в редакторе
        UnityEditor.EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private void OnDisable() {
        UnityEditor.EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }

    private void OnHierarchyChanged() {
        // Сработает при добавлении, удалении или перемещении объекта в иерархии
        if (!Application.isPlaying)
            RefreshSpawnPoints(true);
    }
#endif

    private void RefreshSpawnPoints(bool editorMode) {
        _spawnPoints.Clear();

        for (int i = 0; i < transform.childCount; i++) {
            var child = transform.GetChild(i);

            // если уже есть SpawnPoint — используем его
            if (!child.TryGetComponent(out SpawnPoint sp)) {
                if (editorMode) {
#if UNITY_EDITOR
                    // В редакторе добавляем сразу и помечаем сцену как изменённую
                    sp = UnityEditor.Undo.AddComponent<SpawnPoint>(child.gameObject);
#endif
                } else {
                    // В рантайме добавляем обычным способом
                    sp = child.gameObject.AddComponent<SpawnPoint>();
                }
            }

            if (!_spawnPoints.Contains(sp))
                _spawnPoints.Add(sp);
        }
    }

    public Transform Get(TeamManager.Team team) {
        var checkTeam = TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.TwoTeams;
        var active = _spawnPoints.Where(it => {
                return it.gameObject.activeSelf && (!checkTeam || it.team == team || it.team == TeamManager.Team.None);
            }
        ).ToArray();
        var random = Random.Range(0, active.Length);
        Debug.Log($"[SpawnPoint] Get Random in [0, {active.Length}] = {random}");
        return active[random].transform;
    }
}