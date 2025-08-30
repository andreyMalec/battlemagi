using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class CollisionFootstep : MonoBehaviour {
    public float velocityThreshold = .01f;

    private Vector3 _lastPosition = Vector3.zero;
    private bool _isMoving;
    [Header("Step")] public AudioSource stepAudio;
    public float checkRadius = 0.1f;
    public float yTolerance = 0.01f;
    public float rayLength = 0.3f;

    public float stepCooldown = 0.3f;
    [Header("Layer Settings")] public LayerMask groundLayers = 1; // Default layer
    public AudioClip[] footstepClips;

    private float lastStepTime;
    private Collider[] collision = new Collider[1];

    private void FixedUpdate() {
        float velocity = Vector3.Distance(transform.position, _lastPosition);
        _isMoving = velocity >= velocityThreshold && Math.Abs(transform.position.y - _lastPosition.y) > yTolerance;

        _lastPosition = transform.position;
        CheckGroundContact();
    }

    bool CanPlayFootstep() {
        return Time.time - lastStepTime >= stepCooldown;
    }

    void CheckGroundContact() {
        if (CanPlayFootstep()) {
            RaycastHit hit;
            Vector3 rayStart = transform.position;
            Vector3 rayDirection = Vector3.down;

            if (Physics.Raycast(rayStart, rayDirection, out hit, rayLength, groundLayers)) {
                Debug.Log(hit.normal);
                if (_isMoving) {
                    stepAudio.pitch = Random.Range(0.85f, 1.15f);
                    // PlayOneShot не прерывает текущее воспроизведение!
                    stepAudio.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)]);
                    lastStepTime = Time.time;
                }
            }
            //
            // var size = Physics.OverlapSphereNonAlloc(checkPosition, checkRadius, collision, groundLayers);
            //
            // if (size > 0) {
            //     // if (stepAudio.isPlaying)
            //     //     stepAudio.Stop();
            //     
            //     // stepAudio.PlayOneShot ();
            // }
        }
    }
}