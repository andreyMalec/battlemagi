using UnityEngine;

public class DecalSpawner : MonoBehaviour {
    [SerializeField] private float activationDelay = 0f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float extraScale = 2f;
    [SerializeField] private Texture2D texture;
    [SerializeField] private Color color;
    [SerializeField] private Shader decalShader;
    [SerializeField] private Mesh cubeMesh;
    [SerializeField] private AnimationCurve alphaCurve = new(new Keyframe(0, 1), new Keyframe(1, 0));

    public void Activate(float scale) {
        var decal = new GameObject(this.name + "_Decal");
        decal.transform.position = transform.position;
        decal.transform.rotation = transform.parent.rotation;
        decal.transform.localScale = new Vector3(scale * extraScale, 2, scale * extraScale);
        var mesh = decal.AddComponent<MeshFilter>();
        mesh.mesh = cubeMesh;
        var material = new Material(decalShader);
        material.mainTexture = texture;
        material.color = color;
        var render = decal.AddComponent<MeshRenderer>();
        render.material = material;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
        var destroy = decal.AddComponent<Decal>();
        destroy.Activate(activationDelay, lifetime, alphaCurve);
    }
}

public class Decal : MonoBehaviour {
    private float _lifetime;
    private float _delay;
    private AnimationCurve _alphaCurve;

    private bool _started;
    private float _timer = -1;
    private Material _mat;

    public void Activate(float delay, float lifetime, AnimationCurve alphaCurve) {
        _delay = delay;
        _lifetime = lifetime;
        _timer = lifetime;
        _alphaCurve = alphaCurve;
        _mat = GetComponent<MeshRenderer>().material;
        _started = true;
        _mat.color = new Color(_mat.color.r, _mat.color.g, _mat.color.b, 0f);
    }

    private void Update() {
        if (!_started) return;

        if (_delay > 0) {
            _delay -= Time.deltaTime;
            return;
        }

        if (_timer < 0) {
            Destroy(gameObject);
            return;
        }

        if (_timer > 0) {
            _timer -= Time.deltaTime;
        }

        var alpha = _alphaCurve.Evaluate((_lifetime - _timer) / _lifetime);
        _mat.color = new Color(_mat.color.r, _mat.color.g, _mat.color.b, alpha);
    }
}