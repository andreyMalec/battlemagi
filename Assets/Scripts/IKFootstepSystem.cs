using UnityEngine;

public class IKFootstepSystem : MonoBehaviour {
    [Header("Settings")]
    public AudioSource audioSource;
    public AudioClip[] footstepSounds;
    public LayerMask groundLayer = 1;
    
    public float ikWeight = 0.7f;
    public float rayLength = 0.4f;
    public float minVelocity = 0.4f;
    public float cooldown = 0.15f;

    private Animator animator;
    private Vector3 lastLeftPos, lastRightPos;
    private float lastStepTime;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        ProcessFoot(AvatarIKGoal.LeftFoot, ref lastLeftPos);
        ProcessFoot(AvatarIKGoal.RightFoot, ref lastRightPos);
    }

    void ProcessFoot(AvatarIKGoal foot, ref Vector3 lastPos)
    {
        Vector3 currentPos = animator.GetIKPosition(foot);
        float velocity = (currentPos.y - lastPos.y) / Time.deltaTime;
        
        if (Physics.Raycast(currentPos + Vector3.up * 0.1f, Vector3.down, 
                out RaycastHit hit, rayLength, groundLayer))
        {
            // Устанавливаем IK позицию
            animator.SetIKPositionWeight(foot, ikWeight);
            animator.SetIKPosition(foot, hit.point + Vector3.up * 0.05f);
            
            // Воспроизводим звук при движении вниз
            if (velocity < -minVelocity && Time.time - lastStepTime > cooldown)
            {
                PlaySound();
                lastStepTime = Time.time;
            }
        }
        else
        {
            animator.SetIKPositionWeight(foot, 0);
        }
        
        lastPos = currentPos;
    }

    void PlaySound()
    {
        if (audioSource && footstepSounds.Length > 0)
        {
            audioSource.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)]);
        }
    }
}