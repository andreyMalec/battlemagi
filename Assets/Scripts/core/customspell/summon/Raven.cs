using UnityEngine;

public class Raven : MonoBehaviour {
    [SerializeField] private float moveSpeedThreshold = 0.05f;
    [SerializeField] private float bankingSmooth = 12f;

    private Animator _anim;

    private Vector3 _prevPos;
    private float _songTimer;
    private float _nextSong;

    private int _flyStateHash;
    private int _flyingDirectionHash;
    private int _flyingDirectionYHash;

    private float _bank;

    private void Awake() {
        _anim = GetComponent<Animator>();
        _anim.applyRootMotion = true;

        _flyStateHash = Animator.StringToHash("Base Layer.fly");
        _flyingDirectionHash = Animator.StringToHash("flyingDirectionX");
        _flyingDirectionYHash = Animator.StringToHash("flyingDirectionY");

        _prevPos = transform.position;
        _bank = 0f;
    }

    private void OnEnable() {
        _prevPos = transform.position;
    }

    private void Update() {
        UpdateFlightAnim();
    }

    private void UpdateFlightAnim() {
        var pos = transform.position;
        var delta = pos - _prevPos;
        _prevPos = pos;

        var planar = new Vector3(delta.x, 0f, delta.z);
        var speed = planar.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        var flying = speed > moveSpeedThreshold;

        if (flying) {
            _anim.SetFloat(_flyingDirectionYHash, 1);
            var velDir = planar.sqrMagnitude > 0f ? planar.normalized : transform.forward;
            _bank = Mathf.Lerp(_bank, FindBankingAngle(transform, velDir),
                1f - Mathf.Exp(-bankingSmooth * Time.deltaTime));
            _anim.SetFloat(_flyingDirectionHash, _bank);

            var state = _anim.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != _flyStateHash && state.fullPathHash != _flyStateHash)
                _anim.Play(_flyStateHash);
        } else {
            _bank = 0f;
            _anim.SetFloat(_flyingDirectionYHash, 0);
            _anim.SetFloat(_flyingDirectionHash, 0f);
        }
    }

    private static float FindBankingAngle(Transform self, Vector3 velocityDirPlanar) {
        velocityDirPlanar.y = 0f;
        if (velocityDirPlanar.sqrMagnitude <= 0.000001f)
            return 0f;

        velocityDirPlanar.Normalize();

        var fwd = self.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude <= 0.000001f)
            fwd = Vector3.forward;
        fwd.Normalize();

        var right = new Vector3(fwd.z, 0f, -fwd.x);
        var lateral = Mathf.Clamp(Vector3.Dot(right, velocityDirPlanar), -1f, 1f);
        return lateral;
    }
}