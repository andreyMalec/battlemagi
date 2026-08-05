using System;
using System.Collections;
using UnityEngine;

public class ChargedShotBowstring : MonoBehaviour, ChargingEventHandler {
    [SerializeField] private float chargeDelay = 0.5f;
    [SerializeField] private Transform bowPoint1;
    [SerializeField] private Transform bowPoint2;
    [SerializeField] private LineRenderer lineRenderer;
    private Transform _rightHand;

    private bool _isCharging;

    private void Awake() {
        _rightHand = GetComponentInParent<MeshHands>()?.rightHand;
        if (_rightHand == null)
            _rightHand = GetComponentInParent<MeshController>()?.rightHand;
        lineRenderer.positionCount = 2;
    }

    private void Update() {
        if (_isCharging) {
            lineRenderer.positionCount = 3;
            lineRenderer.SetPosition(0, bowPoint1.position);
            lineRenderer.SetPosition(1, _rightHand.position);
            lineRenderer.SetPosition(2, bowPoint2.position);
        } else {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, bowPoint1.position);
            lineRenderer.SetPosition(1, bowPoint2.position);
        }
    }

    public void StartCharging() {
        StartCoroutine(StartChargingCoroutine());
    }

    private IEnumerator StartChargingCoroutine() {
        yield return new WaitForSeconds(chargeDelay);
        _isCharging = true;
    }

    public void FullyCharged() {
    }
}