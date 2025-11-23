using System.Collections.Generic;
using UnityEngine;

public class ArchetypeAvatar : MonoBehaviour {
    [Header("Target container for avatar instance")]
    [SerializeField] private Transform container; // место, где будет создан аватар

    [SerializeField] private RuntimeAnimatorController controller;

    [SerializeField] private int defaultIndex;

    private int _currentIndex;
    private GameObject _currentAvatar;
    private ArchetypeData _currentData;
    public ArchetypeData CurrentArchetype => _currentData;
    private bool _isInitialized;

    private void Awake() {
        if (container == null) container = transform;
        _currentIndex = defaultIndex;
        SetArchetypeById(_currentIndex);
    }

    // Выбрать архетип по id через базу
    public void SetArchetypeById(int id) {
        var db = ArchetypeDatabase.Instance;
        if (db == null) {
            Debug.LogWarning("ArchetypeAvatar: ArchetypeDatabase.Instance == null");
            return;
        }

        var data = db.GetArchetype(id);
        if (data == null) return;
        SetArchetype(data);
    }

    // Выбрать архетип по индексу списка в базе (для простых кнопок Next/Prev)
    public void SetArchetypeByIndex(int index) {
        var db = ArchetypeDatabase.Instance;
        if (db == null) {
            Debug.LogWarning("ArchetypeAvatar: ArchetypeDatabase.Instance == null");
            return;
        }

        var list = db.archetypes;
        if (list == null || list.Count == 0) return;
        if (index < 0 || index >= list.Count) return;
        var data = list[index];
        if (data == null) return;
        _currentIndex = index;
        SetArchetype(data);
    }

    // Пролистать вперед/назад по базе архетипов
    public void NextArchetype() {
        var (list, currentIndex) = GetCurrentIndexInDb();
        if (list == null || list.Count == 0) return;
        var next = (currentIndex + 1) % list.Count;
        SetArchetypeByIndex(next);
    }

    public void PrevArchetype() {
        var (list, currentIndex) = GetCurrentIndexInDb();
        if (list == null || list.Count == 0) return;
        var prev = (currentIndex - 1 + list.Count) % list.Count;
        SetArchetypeByIndex(prev);
    }

    // Установить конкретный архетип
    public void SetArchetype(ArchetypeData data) {
        if (data == null || data.avatarPrefab == null) {
            Debug.LogWarning("ArchetypeAvatar: data или avatarPrefab == null");
            return;
        }

        if (_currentAvatar != null) {
            Destroy(_currentAvatar);
            _currentAvatar = null;
        }

        // Создаем новый
        _currentAvatar = Instantiate(data.avatarPrefab, container);
        _currentAvatar.transform.localPosition = Vector3.zero;
        _currentAvatar.transform.localRotation = Quaternion.identity;
        _currentAvatar.transform.localScale = Vector3.one;
        var a = _currentAvatar.GetComponent<Animator>();
        a.runtimeAnimatorController = controller;
        _currentAvatar.GetComponent<FootControllerIK>().MaxStepHeight = 0.1f;
        if (_isInitialized)
            a.SetBool("Standing", true);
        _isInitialized = true;
        _currentData = data;
    }

    private (List<ArchetypeData> list, int currentIndex) GetCurrentIndexInDb() {
        var db = ArchetypeDatabase.Instance;
        if (db == null || db.archetypes == null || db.archetypes.Count == 0)
            return (null, -1);
        var list = db.archetypes;
        return (list, _currentIndex);
    }
}