using TMPro;
using UnityEngine;

[RequireComponent(typeof(Bot))]
public class BotStateOverheadView : MonoBehaviour {
    [SerializeField] private Vector3 worldOffset = new(0f, 2.3f, 0f);
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color textColor = new(1f, 1f, 1f, 0.95f);

    private Bot _bot;
    private BotMovementController _movement;
    private BotCombatController _combat;
    private Camera _camera;
    private float _updateTimer;

    private void Awake() {
        _bot = GetComponent<Bot>();
        _movement = GetComponent<BotMovementController>();
        _combat = GetComponent<BotCombatController>();

        if (label == null)
            label = CreateLabel();

        _camera = Camera.main;
        label.gameObject.SetActive(false);
    }

    private void LateUpdate() {
        if (_camera == null || !_camera.isActiveAndEnabled)
            _camera = Camera.main;

        var isSelected = IsSelectedInEditor();
        if (!isSelected) {
            if (label.gameObject.activeSelf)
                label.gameObject.SetActive(false);
            return;
        }

        if (!label.gameObject.activeSelf)
            label.gameObject.SetActive(true);

        label.transform.position = transform.position + worldOffset;
        if (_camera != null) {
            var toCam = label.transform.position - _camera.transform.position;
            if (toCam.sqrMagnitude > 0.0001f)
                label.transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }

        _updateTimer -= Time.deltaTime;
        if (_updateTimer > 0f)
            return;

        _updateTimer = updateInterval;
        var move = _movement != null ? _movement.DebugState : "-";
        var pickup = _movement != null ? _movement.DebugPickup : "-";
        var combat = _combat != null ? _combat.DebugState : "-";
        label.text = $"Bot {_bot.BotId}\nMove: {move}\nPickup: {pickup}\nCombat: {combat}";
    }

    private TMP_Text CreateLabel() {
        var go = new GameObject("BotStateLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = worldOffset;
        var text = go.AddComponent<TextMeshPro>();
        text.fontSize = 3.2f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = textColor;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.18f;
        text.outlineColor = Color.black;
        return text;
    }

    private bool IsSelectedInEditor() {
#if UNITY_EDITOR
        var selected = UnityEditor.Selection.activeGameObject;
        if (selected == null)
            return false;
        return selected == gameObject || selected.transform.IsChildOf(transform);
#else
        return false;
#endif
    }
}


