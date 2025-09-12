using System;
using UnityEngine;

public class FireLight : MonoBehaviour {
    [SerializeField] private Light _light;
    [SerializeField] private AnimationCurve intensityCurve;
    [SerializeField] private AnimationCurve movementCurve;

    private float intensityTime = 0;
    private float movementTime = 0;

    private void Update() {
        var x = movementCurve.Evaluate(movementTime);
        transform.localPosition = new Vector3(x, transform.localPosition.y, -x);
        _light.intensity = intensityCurve.Evaluate(intensityTime);
        
        intensityTime += Time.deltaTime;
        if (intensityTime > intensityCurve.keys[^1].time) {
            intensityTime = 0;
        }

        movementTime += Time.deltaTime;
        if (movementTime > movementCurve.keys[^1].time) {
            movementTime = 0;
        }
    }
}