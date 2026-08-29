using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class TickBasedPredictedMovement : NetworkBehaviour {
    [Header("References")]
    public MovementSettings movementSettings;
    public Transform cameraPivot;

    [Header("Movement")]
    public float movementSpeed = 6f;
    public float jumpStrength = 2f;
    public float lookSensitivity = 2f;
    public float pitchMin = -85f;
    public float pitchMax = 85f;

    [Header("Tick")]
    public int tickRate = 60;
    public int bufferSize = 1024;
    public int maxReplayedTicks = 128;

    [Header("Reconciliation")]
    public float positionErrorThreshold = 0.08f;
    public float yawErrorThreshold = 1.25f;
    public float pitchErrorThreshold = 1.25f;

    public event Action Jumped;

    private struct InputCommand {
        public int Tick;
        public float MoveX;
        public float MoveY;
        public float LookX;
        public float LookY;
        public bool Jump;
    }

    private struct PredictedState {
        public int Tick;
        public Vector3 Position;
        public float Yaw;
        public float Pitch;
        public float VerticalVelocity;
        public bool Grounded;
    }

    private CharacterController _controller;
    private InputCommand[] _inputBuffer;
    private PredictedState[] _stateBuffer;
    private List<InputCommand> _serverPendingInputs;

    private PredictedState _clientState;
    private PredictedState _serverState;
    private PredictedState _remoteTargetState;

    private bool _inputLocked;
    private bool _hasRemoteTargetState;

    private int _clientTick;
    private int _lastServerProcessedTick = -1;

    private float _tickInterval;
    private float _tickAccumulator;

    private void Awake() {
        _controller = GetComponent<CharacterController>();
        _inputBuffer = new InputCommand[Mathf.Max(32, bufferSize)];
        _stateBuffer = new PredictedState[Mathf.Max(32, bufferSize)];
        _serverPendingInputs = new List<InputCommand>(32);
        _tickInterval = 1f / Mathf.Max(1, tickRate);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        ResetRuntimeState();
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        _serverPendingInputs.Clear();
        _hasRemoteTargetState = false;
    }

    private void Update() {
        if (!IsSpawned)
            return;

        _tickAccumulator += Time.deltaTime;
        while (_tickAccumulator >= _tickInterval) {
            _tickAccumulator -= _tickInterval;
            TickSimulation();
        }

        if (!IsOwner)
            SmoothRemoteState(Time.deltaTime);
    }

    private void TickSimulation() {
        if (IsOwner)
            TickOwner();

        if (IsServer && !IsOwner)
            TickServerForRemoteOwner();
    }

    private void TickOwner() {
        _clientTick++;

        InputCommand input = CaptureInput(_clientTick);
        Simulate(ref _clientState, input, _tickInterval, true);

        int bufferIndex = ToBufferIndex(input.Tick);
        _inputBuffer[bufferIndex] = input;
        _stateBuffer[bufferIndex] = _clientState;

        if (IsServer) {
            _serverState = _clientState;
            _lastServerProcessedTick = _serverState.Tick;
            BroadcastServerState(_serverState);
            return;
        }

        SubmitInputServerRpc(
            input.Tick,
            input.MoveX,
            input.MoveY,
            input.LookX,
            input.LookY,
            input.Jump
        );
    }

    private void TickServerForRemoteOwner() {
        for (int i = 0; i < _serverPendingInputs.Count; i++) {
            InputCommand command = _serverPendingInputs[i];
            if (command.Tick <= _lastServerProcessedTick)
                continue;

            Simulate(ref _serverState, command, _tickInterval, true);
            _lastServerProcessedTick = command.Tick;
            BroadcastServerState(_serverState);
        }

        _serverPendingInputs.Clear();
    }

    private InputCommand CaptureInput(int tick) {
        if (_inputLocked || Cursor.lockState != CursorLockMode.Locked) {
            return new InputCommand {
                Tick = tick
            };
        }

        return new InputCommand {
            Tick = tick,
            MoveX = Input.GetAxisRaw("Horizontal"),
            MoveY = Input.GetAxisRaw("Vertical"),
            LookX = Input.GetAxisRaw("Mouse X"),
            LookY = Input.GetAxisRaw("Mouse Y"),
            Jump = Input.GetKeyDown(movementSettings.jumpKey)
        };
    }

    private void Simulate(ref PredictedState state, InputCommand input, float dt, bool applyToTransform) {
        float yaw = state.Yaw + input.LookX * lookSensitivity;
        float pitch = Mathf.Clamp(state.Pitch - input.LookY * lookSensitivity, pitchMin, pitchMax);

        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);

        Vector3 moveInput = new Vector3(input.MoveX, 0f, input.MoveY);
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        Vector3 worldHorizontalVelocity = yawRotation * moveInput * movementSpeed;

        float verticalVelocity = state.VerticalVelocity;
        bool grounded = state.Grounded;

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (input.Jump && grounded) {
            verticalVelocity = Mathf.Sqrt(jumpStrength * -2f * movementSettings.gravity);
            Jumped?.Invoke();
        }

        float gravityMultiplier = verticalVelocity < 0f ? movementSettings.fallGravityMultiplier : 1f;
        verticalVelocity += movementSettings.gravity * gravityMultiplier * dt;

        Vector3 motion = (worldHorizontalVelocity + Vector3.up * verticalVelocity) * dt;
        CollisionFlags flags = _controller.Move(motion);

        bool groundedNow = (flags & CollisionFlags.Below) != 0 || _controller.isGrounded;
        if (groundedNow && verticalVelocity < 0f)
            verticalVelocity = -2f;

        state.Tick = input.Tick;
        state.Position = transform.position;
        state.Yaw = yaw;
        state.Pitch = pitch;
        state.VerticalVelocity = verticalVelocity;
        state.Grounded = groundedNow;

        if (applyToTransform)
            ApplyVisualRotation(yaw, pitch);
    }

    private void ApplyVisualRotation(float yaw, float pitch) {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    private void SubmitInputServerRpc(int tick, float moveX, float moveY, float lookX, float lookY, bool jump) {
        InputCommand input = new InputCommand {
            Tick = tick,
            MoveX = moveX,
            MoveY = moveY,
            LookX = lookX,
            LookY = lookY,
            Jump = jump
        };

        if (tick <= _lastServerProcessedTick)
            return;

        InsertServerInputOrdered(input);
    }

    private void InsertServerInputOrdered(InputCommand input) {
        for (int i = _serverPendingInputs.Count - 1; i >= 0; i--) {
            if (_serverPendingInputs[i].Tick == input.Tick)
                return;
            if (_serverPendingInputs[i].Tick < input.Tick) {
                _serverPendingInputs.Insert(i + 1, input);
                return;
            }
        }

        _serverPendingInputs.Insert(0, input);
    }

    private void BroadcastServerState(PredictedState state) {
        ReceiveServerStateClientRpc(
            state.Tick,
            state.Position,
            state.Yaw,
            state.Pitch,
            state.VerticalVelocity,
            state.Grounded
        );
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void ReceiveServerStateClientRpc(int tick, Vector3 position, float yaw, float pitch, float verticalVelocity,
        bool grounded) {
        PredictedState authoritative = new PredictedState {
            Tick = tick,
            Position = position,
            Yaw = yaw,
            Pitch = pitch,
            VerticalVelocity = verticalVelocity,
            Grounded = grounded
        };

        if (!IsOwner) {
            _remoteTargetState = authoritative;
            _hasRemoteTargetState = true;
            return;
        }

        Reconcile(authoritative);
    }

    private void Reconcile(PredictedState authoritative) {
        if (authoritative.Tick <= 0)
            return;

        int index = ToBufferIndex(authoritative.Tick);
        PredictedState predicted = _stateBuffer[index];
        if (predicted.Tick != authoritative.Tick)
            return;

        float positionError = Vector3.Distance(predicted.Position, authoritative.Position);
        float yawError = Mathf.Abs(Mathf.DeltaAngle(predicted.Yaw, authoritative.Yaw));
        float pitchError = Mathf.Abs(Mathf.DeltaAngle(predicted.Pitch, authoritative.Pitch));

        if (positionError <= positionErrorThreshold && yawError <= yawErrorThreshold && pitchError <= pitchErrorThreshold)
            return;

        _clientState = authoritative;
        ApplyState(_clientState);

        int replayed = 0;
        int replayFromTick = authoritative.Tick + 1;

        for (int tick = replayFromTick; tick <= _clientTick; tick++) {
            if (replayed >= maxReplayedTicks)
                break;

            int replayIndex = ToBufferIndex(tick);
            InputCommand input = _inputBuffer[replayIndex];
            if (input.Tick != tick)
                continue;

            Simulate(ref _clientState, input, _tickInterval, true);
            _stateBuffer[replayIndex] = _clientState;
            replayed++;
        }
    }

    private void SmoothRemoteState(float dt) {
        if (!_hasRemoteTargetState)
            return;

        float blend = Mathf.Clamp01(dt * 20f);
        transform.position = Vector3.Lerp(transform.position, _remoteTargetState.Position, blend);

        float yaw = Mathf.LerpAngle(transform.eulerAngles.y, _remoteTargetState.Yaw, blend);
        float pitch = Mathf.LerpAngle(cameraPivot.localEulerAngles.x, _remoteTargetState.Pitch, blend);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void ApplyState(PredictedState state) {
        _controller.enabled = false;
        transform.position = state.Position;
        ApplyVisualRotation(state.Yaw, state.Pitch);
        _controller.enabled = true;
    }

    private int ToBufferIndex(int tick) {
        int size = _inputBuffer.Length;
        int index = tick % size;
        return index < 0 ? index + size : index;
    }

    private void ResetRuntimeState() {
        _tickAccumulator = 0f;
        _clientTick = 0;
        _lastServerProcessedTick = -1;

        float yaw = transform.eulerAngles.y;
        float pitch = cameraPivot.localEulerAngles.x;
        if (pitch > 180f)
            pitch -= 360f;

        PredictedState initial = new PredictedState {
            Tick = 0,
            Position = transform.position,
            Yaw = yaw,
            Pitch = pitch,
            VerticalVelocity = 0f,
            Grounded = _controller.isGrounded
        };

        _clientState = initial;
        _serverState = initial;
        _remoteTargetState = initial;

        Array.Clear(_inputBuffer, 0, _inputBuffer.Length);
        Array.Clear(_stateBuffer, 0, _stateBuffer.Length);
        _serverPendingInputs.Clear();
        _hasRemoteTargetState = false;
    }

    public void SetInputLocked(bool locked) {
        _inputLocked = locked;
    }
}

