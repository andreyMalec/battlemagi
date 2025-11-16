using System;
using Unity.Netcode.Components;
using UnityEngine;
using Random = UnityEngine.Random;

public class FirstPersonSounds : MonoBehaviour {
    private static readonly int Footstep = Animator.StringToHash("Footstep");
    private static readonly int Jump = Animator.StringToHash("Jump");

    public AudioSource stepsAudio;
    public AudioSource jumpsAudio;
    public GroundCheck groundCheck;
    private Animator animator;

    [SerializeField] private float stepsPitchFrom = 0.8f;
    [SerializeField] private float stepsPitchTo = 1.2f;

    [Header("Clips")]
    public AudioClip[] steps;

    public AudioClip[] jumps;

    private float lastStep;
    private float lastJump;

    public void BindAvatar(Animator a) {
        animator = a;
    }

    private void Update() {
        UpdateStep();
        UpdateJump();
    }

    private void UpdateStep() {
        if (!groundCheck.isGrounded)
            return;

        if (animator == null) return;
        var step = animator.GetFloat(Footstep);
        if (Math.Abs(step) < 0.00001)
            step = 0f;

        if (lastStep > 0 && step < 0f || lastStep < 0 && step > 0f) {
            PlayStep();
        }

        lastStep = step;
    }

    private void UpdateJump() {
        if (animator == null) return;
        var jump = animator.GetFloat(Jump);
        if (Math.Abs(jump) < 0.00001)
            jump = 0f;

        if (lastJump > 0 && jump < 0f || lastJump < 0 && jump > 0f) {
            PlayJump();
        }

        lastJump = jump;
    }

    public void PlayStep() {
        stepsAudio.pitch = Random.Range(stepsPitchFrom, stepsPitchTo);
        stepsAudio.Play(steps);
    }

    public void PlayJump() {
        jumpsAudio.pitch = Random.Range(0.9f, 1.1f);
        jumpsAudio.Play(jumps);
    }
}