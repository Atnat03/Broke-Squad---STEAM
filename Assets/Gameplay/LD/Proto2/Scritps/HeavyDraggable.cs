using System;
using Gameplay.Controller;

using Gameplay.Controller;
using Gameplay.LD.Proto2;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    [RequireComponent(typeof(Rigidbody))]
    public class HeavyDraggable : NetworkBehaviour
    {
        private const ulong NoHolder = ulong.MaxValue;

        [Header("Handles")]
        [SerializeField] private DragHandle[] handles;

        [Header("Movement")]
        [SerializeField] private Vector3 localAxis = Vector3.forward;
        [SerializeField] private float minOffset = 0f;
        [SerializeField] private float maxOffset = 5f;
        [SerializeField] private float maxSpeed = 1.5f;
        [SerializeField] private float acceleration = 3f;

        [Header("Input")]
        [SerializeField, Range(0.05f, 0.9f)] private float inputDeadzone = 0.3f;
        [SerializeField] private float inputTimeout = 0.3f;
        [SerializeField] private float inputSendRate = 20f;
        
        private NetworkList<ulong> _holders;
        private Rigidbody _rb;
        private Vector3 _origin;
        private Vector3 _worldAxis;
        
        private float[] _inputs;
        private float[] _inputTimes;
        private float _offset;
        private float _velocity;
        
        private PlayerController _localPlayer;
        private int _localIndex = -1;
        private float _sendTimer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _holders = new NetworkList<ulong>();
            for (int i = 0; i < handles.Length; i++) handles[i].Init(this, i);
        }

        public override void OnNetworkSpawn()
        {
            _origin = _rb.position;
            _worldAxis = transform.TransformDirection(localAxis).normalized;

            if (IsServer)
            {
                _inputs = new float[handles.Length];
                _inputTimes = new float[handles.Length];
                _holders.Clear();
                for (int i = 0; i < handles.Length; i++) _holders.Add(NoHolder);
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }

            _holders.OnListChanged += OnHoldersChanged;
        }

        public override void OnNetworkDespawn()
        {
            _holders.OnListChanged -= OnHoldersChanged;
            if (IsServer && NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            if (_localIndex >= 0) EndLocalHold();
        }

        public bool IsHandleFree(int index) => index < _holders.Count && _holders[index] == NoHolder;

        #region Requests (called by DragHandle on the local client)

        public void RequestHold(int index)
        {
            if (_localIndex >= 0) return;
            RequestHoldRpc(index);
        }

        public void RequestRelease(int index) => RequestReleaseRpc(index);

        #endregion

        #region Server

        [Rpc(SendTo.Server)]
        private void RequestHoldRpc(int index, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (index < 0 || index >= _holders.Count || _holders[index] != NoHolder) return;
            
            foreach (var holder in _holders)
                if (holder == sender) return;

            _holders[index] = sender;
            _inputs[index] = 0f;
            _inputTimes[index] = Time.time;
        }

        [Rpc(SendTo.Server)]
        private void RequestReleaseRpc(int index, RpcParams rpcParams = default)
        {
            if (index < 0 || index >= _holders.Count) return;
            if (_holders[index] != rpcParams.Receive.SenderClientId) return;
            Release(index);
        }

        [Rpc(SendTo.Server)]
        private void SubmitInputRpc(int index, float value, RpcParams rpcParams = default)
        {
            if (index < 0 || index >= _holders.Count) return;
            if (_holders[index] != rpcParams.Receive.SenderClientId) return;
            _inputs[index] = Mathf.Clamp(value, -1f, 1f);
            _inputTimes[index] = Time.time;
        }

        private void Release(int index)
        {
            _holders[index] = NoHolder;
            _inputs[index] = 0f;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            for (int i = 0; i < _holders.Count; i++)
                if (_holders[i] == clientId) Release(i);
        }

        private void ServerTick(float dt)
        {
            float target = 0f;

            if (TryGetDragDirection(out float dir, out float strength)) target = dir * maxSpeed * strength;

            _velocity = Mathf.MoveTowards(_velocity, target, acceleration * dt);
            float newOffset = Mathf.Clamp(_offset + _velocity * dt, minOffset, maxOffset);
            if (Mathf.Approximately(newOffset, _offset)) _velocity = 0f;
            _offset = newOffset;

            _rb.MovePosition(_origin + _worldAxis * _offset);
        }

       
        private bool TryGetDragDirection(out float dir, out float strength)
        {
            dir = 0f;
            strength = 1f;
            if (handles.Length == 0) return false;

            for (int i = 0; i < _holders.Count; i++)
            {
                if (_holders[i] == NoHolder) return false;
                if (Time.time - _inputTimes[i] > inputTimeout) return false;

                float v = _inputs[i];
                if (Mathf.Abs(v) < inputDeadzone) return false;

                float s = Mathf.Sign(v);
                if (dir == 0f) dir = s;
                else if (!Mathf.Approximately(dir, s)) return false;

                strength = Mathf.Min(strength, Mathf.Abs(v)); 
            }
            return true;
        }

        #endregion

        #region Local client

        private void OnHoldersChanged(NetworkListEvent<ulong> _) => RefreshLocalHold();

        private void RefreshLocalHold()
        {
            if (NetworkManager == null) return;
            ulong me = NetworkManager.LocalClientId;

            int idx = -1;
            for (int i = 0; i < _holders.Count; i++)
                if (_holders[i] == me) { idx = i; break; }

            if (idx == _localIndex) return;
            if (_localIndex >= 0) EndLocalHold();
            if (idx >= 0) BeginLocalHold(idx);
        }

        private void BeginLocalHold(int index)
        {
            var playerObj = NetworkManager.LocalClient?.PlayerObject;
            if (playerObj == null || !playerObj.TryGetComponent(out _localPlayer)) return;

            _localIndex = index;
            _sendTimer = 0f;
            _localPlayer.PushLock(); 
            _localPlayer.SetYaw(handles[index].SnapPoint.eulerAngles.y);
        }

        private void EndLocalHold()
        {
            if (_localPlayer != null) _localPlayer.PopLock();
            _localPlayer = null;
            _localIndex = -1;
        }

        private void LocalTick(float dt)
        {
            if (_localIndex < 0 || _localPlayer == null) return;

            if (_localPlayer.IsDown || _localPlayer.IsStunned)
            {
                RequestRelease(_localIndex);
                return;
            }

            Vector3 pos = handles[_localIndex].SnapPoint.position;
            pos = new Vector3(pos.x, _localPlayer.transform.position.y, pos.z);
            _localPlayer.HoldAt(pos);

            _sendTimer -= dt;
            if (_sendTimer <= 0f)
            {
                _sendTimer = 1f / inputSendRate;
                float v = Vector3.Dot(_localPlayer.RawDesiredDirection, _worldAxis);
                SubmitInputRpc(_localIndex, v);
            }
        }

        #endregion

        private void FixedUpdate()
        {
            if (!IsSpawned) return;
            float dt = Time.fixedDeltaTime;
            if (IsServer) ServerTick(dt);
            LocalTick(dt);
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 axis = transform.TransformDirection(localAxis).normalized;
            if (axis == Vector3.zero) return;

            bool playing = Application.isPlaying && IsSpawned;
            Vector3 origin = playing ? _origin : transform.position;
            float current = playing ? _offset : 0f;

            Vector3 minPos = origin + axis * minOffset;
            Vector3 maxPos = origin + axis * maxOffset;
            Vector3 curPos = origin + axis * current;
            
            Gizmos.color = Color.white;
            Gizmos.DrawLine(minPos, maxPos);
            
            Bounds b = GetLocalBounds(out Vector3 centerOffset);
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(minPos + centerOffset, b.size);
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(maxPos + centerOffset, b.size);
            if (playing)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(curPos + centerOffset, b.size);
            }
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(minPos, 0.1f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(maxPos, 0.1f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(curPos, 0.08f);
            
            Gizmos.color = Color.cyan;
            DrawArrow(curPos, axis * 0.8f);
            DrawArrow(curPos, -axis * 0.8f);
        }

        private Bounds GetLocalBounds(out Vector3 centerOffset)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                centerOffset = Vector3.zero;
                return new Bounds(transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            centerOffset = bounds.center - transform.position;
            return bounds;
        }

        private static void DrawArrow(Vector3 from, Vector3 dir)
        {
            Vector3 to = from + dir;
            Gizmos.DrawLine(from, to);

            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(dir, Vector3.right);
            side = side.normalized * dir.magnitude * 0.15f;
            Vector3 back = -dir * 0.2f;

            Gizmos.DrawLine(to, to + back + side);
            Gizmos.DrawLine(to, to + back - side);
        }
#endif
    }
}
