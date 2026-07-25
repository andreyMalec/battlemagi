using System;
using UnityEngine;

public class Bookmark : MonoBehaviour {
    [SerializeField] private float hoverUp = 0.2f;
    [SerializeField] private float hoverUpTime = 0.5f;

    public event Action OnClick;

    private Collider _collider;
    private Camera _camera;
    private Vector3 _initialPosition;
    private bool _isHovered = false;
    private float _hoverUpTimer;

    private void Awake() {
        _collider = GetComponent<Collider>();
        _camera = Camera.main;
        _initialPosition = transform.localPosition;
    }

    private void Update() {
        if (_hoverUpTimer > 0) {
            _hoverUpTimer -= Time.deltaTime;
            return;
        }

        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (_collider.Raycast(ray, out RaycastHit hit, 1000f)) {
            _isHovered = true;
            _hoverUpTimer = hoverUpTime;
        } else {
            _isHovered = false;
        }

        if (_isHovered) {
            transform.localPosition = _initialPosition + Vector3.forward * hoverUp;
        } else {
            transform.localPosition = _initialPosition;
        }

        if (Input.GetMouseButton(0) && _isHovered) {
            OnClick?.Invoke();
        }
    }
}