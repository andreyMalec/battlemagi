using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Portal : MonoBehaviour {
    [SerializeField] private Portal linked;
    [SerializeField] private float reactivationTimeout = 1f;
    [SerializeField] private float exitOffset = 1.2f;
    [SerializeField] private AudioSource audioSource;

    [Header("Portal View")]
    [SerializeField] private Camera portalCamera;

    [SerializeField] private Renderer screenRenderer;
    [SerializeField] private string screenTextureProperty = "_MainTex";
    [SerializeField] private Vector2Int textureSize = new(1024, 1024);

    [SerializeField] private float maxViewDistance = 40f;

    [SerializeField] private LayerMask portalLayer;

    private RenderTexture _rt;

    private readonly Dictionary<ulong, float> _nextAllowedTime = new();

    private struct PendingTeleport {
        public Transform target;
        public Vector3 position;
        public Quaternion rotation;
        public FirstPersonLook look;
    }

    private readonly List<PendingTeleport> _pendingTeleports = new();
    private int _baseCullingMask;

    private void Awake() {
        EnsureLinkedRenderTexture();
        ApplyLinkedTextureToScreen();
        portalCamera.depth = 50;
        _baseCullingMask = portalCamera.cullingMask;
    }

    private void OnEnable() {
        EnsureLinkedRenderTexture();
        ApplyLinkedTextureToScreen();
    }

    private void OnDisable() {
        ReleaseRenderTexture();
    }

    private void FixedUpdate() {
        ProcessTeleports();

        var viewer = Camera.main;
        if (viewer == null)
            return;

        var linkedCam = linked.portalCamera;
        if (linkedCam == null)
            return;

        if ((viewer.transform.position - transform.position).sqrMagnitude > maxViewDistance * maxViewDistance) {
            linkedCam.enabled = false;
            return;
        }

        linked.EnsureOwnRenderTexture();
        ApplyLinkedTextureToScreen();

        linkedCam.targetTexture = linked._rt;
        linkedCam.enabled = true;

        linkedCam.cullingMask = linked._baseCullingMask & ~portalLayer.value;

        var viewerT = viewer.transform;

        var localPos = transform.InverseTransformPoint(viewerT.position);
        var localRot = Quaternion.Inverse(transform.rotation) * viewerT.rotation;

        var flip = Quaternion.AngleAxis(180f, Vector3.up);
        localPos = flip * localPos;
        localRot = flip * localRot;

        linkedCam.transform.position = linked.transform.TransformPoint(localPos);
        linkedCam.transform.rotation = linked.transform.rotation * localRot;

        linkedCam.fieldOfView = viewer.fieldOfView;
        linkedCam.nearClipPlane = viewer.nearClipPlane;
        linkedCam.farClipPlane = viewer.farClipPlane;
    }

    private void ProcessTeleports() {
        if (_pendingTeleports.Count == 0)
            return;

        for (var i = 0; i < _pendingTeleports.Count; i++) {
            var tp = _pendingTeleports[i];
            tp.target.position = tp.position;
            tp.look.ApplyInitialRotation(tp.rotation);
        }

        _pendingTeleports.Clear();
    }

    private void EnsureLinkedRenderTexture() {
        if (linked == null)
            return;
        linked.EnsureOwnRenderTexture();
    }

    private void EnsureOwnRenderTexture() {
        if (portalCamera == null)
            return;

        var w = Mathf.Max(16, textureSize.x);
        var h = Mathf.Max(16, textureSize.y);

        if (_rt == null || _rt.width != w || _rt.height != h) {
            ReleaseRenderTexture();
            _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) {
                name = $"PortalRT_{GetInstanceID()}",
                useMipMap = false,
                autoGenerateMips = false
            };
            _rt.Create();
        }

        portalCamera.targetTexture = _rt;
    }

    private void ApplyLinkedTextureToScreen() {
        if (screenRenderer == null)
            return;
        if (linked == null)
            return;
        if (linked._rt == null)
            return;

        screenRenderer.material.SetTexture(screenTextureProperty, linked._rt);
    }

    private void ReleaseRenderTexture() {
        if (portalCamera != null)
            portalCamera.targetTexture = null;

        if (_rt != null) {
            if (_rt.IsCreated())
                _rt.Release();
            Destroy(_rt);
            _rt = null;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!DamageUtils.TryGetOwnerFromCollider(other, out var player, out var owner))
            return;
        if (!player.IsOwner)
            return;
        if (_nextAllowedTime.TryGetValue(owner, out var nextTime) && Time.time < nextTime)
            return;
        if (linked._nextAllowedTime.TryGetValue(owner, out var linkedNext) && Time.time < linkedNext)
            return;

        _nextAllowedTime[owner] = Time.time + reactivationTimeout;
        linked._nextAllowedTime[owner] = Time.time + reactivationTimeout;

        var exitPos = linked.transform.position + linked.transform.forward * exitOffset;
        var rot = linked.transform.rotation;

        var look = player.GetComponent<FirstPersonLook>();

        _pendingTeleports.Add(new PendingTeleport {
            target = player.transform.root,
            position = exitPos,
            rotation = rot,
            look = look
        });

        audioSource.Play();
        linked.audioSource.Play();
        Debug.Log($"[Portal] {player.gameObject.name} - scheduled teleport to {exitPos}");
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.cyan;

        var start = transform.position;
        var end = transform.position +
                  new Vector3(transform.forward.x * 2, transform.forward.y, transform.forward.z * 2);
        Gizmos.DrawLine(start, end);

        var dir = end - start;
        if (dir.sqrMagnitude < 0.000001f) {
            return;
        }

        dir.Normalize();

        var headLength = 0.6f;
        var headAngle = 25f;

        var axis = Vector3.Cross(dir, Vector3.up);
        if (axis.sqrMagnitude < 0.000001f) {
            axis = Vector3.Cross(dir, Vector3.forward);
        }

        axis.Normalize();

        var right = Quaternion.AngleAxis(headAngle, axis) * (-dir);
        var left = Quaternion.AngleAxis(-headAngle, axis) * (-dir);
        Gizmos.DrawLine(end, end + right * headLength);
        Gizmos.DrawLine(end, end + left * headLength);
    }
}