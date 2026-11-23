    using Gameplay.Controller;
    using Gameplay.LD.Proto2;
    using Unity.Netcode;
    using UnityEngine;

    namespace Gameplay.Items
    {
        [System.Serializable]
        public class DragAxis
        {
            public string name = "Axis";
            public Vector3 localAxis = Vector3.forward;
            public float minOffset = 0f;
            public float maxOffset = 5f;
            public float maxSpeed = 1.5f;
            public float acceleration = 3f;
            [Min(1)]public int playersRequired = 2;
            
        }

        [RequireComponent(typeof(Rigidbody))]
        public class HeavyDraggable : NetworkBehaviour
        {
            private const ulong NoHolder = ulong.MaxValue;
            private const int MaxAxes = 3; 
            
            
            
            [SerializeField] private DragHandle[] handles;
            
            [SerializeField] private DragAxis[] axes =
            {
                new DragAxis { name = "X", localAxis = Vector3.right },
                new DragAxis { name = "Z", localAxis = Vector3.forward },
            };

            [Header("Input")]
            [SerializeField, Range(0.05f, 0.9f)] private float inputDeadzone = 0.3f;
            [SerializeField] private float inputTimeout = 0.3f;
            [SerializeField] private float inputSendRate = 20f;
            
            [Header("Collision")]
            [SerializeField] private float collisionSkin = 0.02f;

            
            private readonly NetworkVariable<Vector3> _netOffsets = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
            
            private NetworkList<ulong> _holders;

            private Rigidbody _rb;
            private Vector3 _origin;
            private Vector3[] _worldAxes;
            
            private float[] _inputs;
            private float[] _inputTimes;
            private float[] _offsets;
            private float[] _velocities;
            private float[] _displayOffsets;
            private Vector3[] _snapOffsets;

            
            private PlayerController _localPlayer;
            private int _localIndex = -1;
            private float _sendTimer;

            private static readonly Color[] AxisColors =
            {
                new Color(1f, 0.35f, 0.35f), new Color(0.35f, 0.6f, 1f), new Color(0.4f, 1f, 0.4f)
            };

            private void OnValidate()
            {
                if (axes != null && axes.Length > MaxAxes) System.Array.Resize(ref axes, MaxAxes);
            }

            private void Awake()
            {
                _rb = GetComponent<Rigidbody>();
                _rb.isKinematic = true;
                _rb.interpolation = RigidbodyInterpolation.Interpolate;
                _holders = new NetworkList<ulong>();

                int n = axes.Length;
                _offsets = new float[n];
                _velocities = new float[n];
                _displayOffsets = new float[n];

                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i].Init(this, i);
                    if (handles[i].Axis < 0 || handles[i].Axis >= n)
                        Debug.LogError($"{name}: handle {i} has an invalid axis index.", handles[i]);
                }
            }

            public override void OnNetworkSpawn()
            {
                _origin = _rb.position;
                _worldAxes = new Vector3[axes.Length];
                for (int a = 0; a < axes.Length; a++)
                    _worldAxes[a] = transform.TransformDirection(axes[a].localAxis).normalized;

                if (!IsServer)
                {
                    for (int a = 0; a < axes.Length; a++) _displayOffsets[a] = Comp(_netOffsets.Value, a);
                    _rb.position = PositionFor(_displayOffsets);
                }

                if (IsServer)
                {
                    _snapOffsets = new Vector3[handles.Length];
                    for (int i = 0; i < handles.Length; i++)
                        _snapOffsets[i] = handles[i].SnapPoint.position - _rb.position;
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
                if (IsServer && NetworkManager != null)
                    NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                if (_localIndex >= 0) EndLocalHold();
            }

            private bool IsHolder(ulong clientId)
            {
                foreach (var holder in _holders)
                    if (holder == clientId) return true;
                return false;
            }
            private bool IsHolderCollider(Collider c)
            {
                var player = c.GetComponentInParent<PlayerController>();
                return player != null && IsHolder(player.OwnerClientId);
            }
            
            private float AllowedDistance(Vector3 dir, float dist)
            {
                float allowed = dist;
                
                var hits = _rb.SweepTestAll(dir, dist + collisionSkin, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    if (h.collider == null) continue;
                    if (Vector3.Dot(h.normal, dir) >= 0f) continue;
                    if (IsHolderCollider(h.collider)) continue;
                    allowed = Mathf.Min(allowed, Mathf.Max(0f, h.distance - collisionSkin));
                }

                
                for (int i = 0; i < _holders.Count; i++)
                {
                    if (_holders[i] == NoHolder) continue;
                    var po = NetworkManager.SpawnManager.GetPlayerNetworkObject(_holders[i]);
                    if (po == null) continue;

                    var cap = po.GetComponentInChildren<CapsuleCollider>();
                    if (cap == null || cap.isTrigger) continue;

                    GetCapsuleWorld(cap, collisionSkin, out Vector3 p1, out Vector3 p2, out float r);
                    
                    Vector3 shift = (_rb.position + _snapOffsets[i]) - po.transform.position;
                    p1 += shift;
                    p2 += shift;

                    var pHits = Physics.CapsuleCastAll(p1, p2, r, dir, dist + collisionSkin,
                        ~0, QueryTriggerInteraction.Ignore);

                    foreach (var h in pHits)
                    {
                        if (h.collider == null) continue;
                        if (h.distance <= 0f) continue;                             
                        if (Vector3.Dot(h.normal, dir) >= 0f) continue;
                        if (h.collider.transform.IsChildOf(transform)) continue;     
                        if (IsHolderCollider(h.collider)) continue;                
                        allowed = Mathf.Min(allowed, Mathf.Max(0f, h.distance - collisionSkin));
                    }
                }

                return allowed;
            }
            public bool IsHandleFree(int index) => index < _holders.Count && _holders[index] == NoHolder;

            private  void GetCapsuleWorld(CapsuleCollider cap, float shrink, out Vector3 p1, out Vector3 p2, out float radius)
            {
                Transform t = cap.transform;
                Vector3 s = t.lossyScale;
                s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

                int dirIdx = cap.direction; 
                Vector3 localDir = dirIdx == 0 ? Vector3.right : dirIdx == 1 ? Vector3.up : Vector3.forward;
                float lenScale = dirIdx == 0 ? s.x : dirIdx == 1 ? s.y : s.z;
                float radScale = dirIdx == 0 ? Mathf.Max(s.y, s.z)
                    : dirIdx == 1 ? Mathf.Max(s.x, s.z)
                    : Mathf.Max(s.x, s.y);

                radius = Mathf.Max(0.01f, cap.radius * radScale - shrink);
                float half = Mathf.Max(radius, cap.height * 0.5f * lenScale - shrink);

                Vector3 center = t.TransformPoint(cap.center);
                Vector3 axis = t.TransformDirection(localDir).normalized;
                p1 = center + axis * (half - radius);
                p2 = center - axis * (half - radius);
            }
            private Vector3 PositionFor(float[] offsets)
            {
                Vector3 p = _origin;
                for (int a = 0; a < axes.Length; a++) p += _worldAxes[a] * offsets[a];
                return p;
            }

            private static float Comp(Vector3 v, int i) => i == 0 ? v.x : i == 1 ? v.y : v.z;

            private static void SetComp(ref Vector3 v, int i, float f)
            {
                if (i == 0) v.x = f;
                else if (i == 1) v.y = f;
                else v.z = f;
            }

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
                
                for (int i = 0; i < _holders.Count; i++)
                    if (_holders[i] == sender) return;

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
                Vector3 net = Vector3.zero;

                for (int a = 0; a < axes.Length; a++)
                {
                    DragAxis ax = axes[a];

                    float target = 0f;
                    bool dragging = TryGetDragDirection(a, out float dir, out float strength);
                    if (dragging) target = dir * ax.maxSpeed * strength;

                    if (!dragging) _velocities[a] = 0f;
                    else _velocities[a] = Mathf.MoveTowards(_velocities[a], target, ax.acceleration * dt);

                    float desired = Mathf.Clamp(_offsets[a] + _velocities[a] * dt, ax.minOffset, ax.maxOffset);

                    float delta = desired - _offsets[a];
                    if (Mathf.Abs(delta) > 1e-5f)
                    {
                        float sign = Mathf.Sign(delta);
                        float allowed = AllowedDistance(_worldAxes[a] * sign, Mathf.Abs(delta));
                        if (allowed < Mathf.Abs(delta) - 1e-5f)
                        {
                            desired = _offsets[a] + sign * allowed;
                            _velocities[a] = 0f; 
                        }
                    }

                    if (Mathf.Approximately(desired, _offsets[a])) _velocities[a] = 0f;
                    _offsets[a] = desired;
                    SetComp(ref net, a, desired);
                }

                _rb.MovePosition(PositionFor(_offsets));
                _netOffsets.Value = net;
            }

            
            private bool TryGetDragDirection(int axis, out float dir, out float strength)
            {
                dir = 0f;
                strength = 1f;
                int total = 0, active = 0;

                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i].Axis != axis) continue;
                    total++;

                    if (_holders[i] == NoHolder) continue;                       
                    if (Time.time - _inputTimes[i] > inputTimeout) continue;     

                    float v = _inputs[i];
                    if (Mathf.Abs(v) < inputDeadzone) continue;                  

                    float s = Mathf.Sign(v);
                    if (dir == 0f) dir = s;
                    else if (!Mathf.Approximately(dir, s))                        
                    {
                        dir = 0f;
                        return false;
                    }

                    active++;
                    strength = Mathf.Min(strength, Mathf.Abs(v));                 
                }

                if (total == 0) return false;                                     

                int required = Mathf.Min(Mathf.Max(1, axes[axis].playersRequired), total);
                if (active < required)
                {
                    dir = 0f;
                    return false;
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

              
                _localPlayer.HoldAt(handles[_localIndex].SnapPoint.position);

                _sendTimer -= dt;
                if (_sendTimer <= 0f)
                {
                    _sendTimer = 1f / inputSendRate;
                    Vector3 axis = _worldAxes[handles[_localIndex].Axis];
                    SubmitInputRpc(_localIndex, Vector3.Dot(_localPlayer.RawDesiredDirection, axis));
                }
            }

            #endregion

            
            private void ClientFollow(float dt)
            {
                float k = 1f - Mathf.Exp(-20f * dt);
                for (int a = 0; a < axes.Length; a++)
                    _displayOffsets[a] = Mathf.Lerp(_displayOffsets[a], Comp(_netOffsets.Value, a), k);

                _rb.MovePosition(PositionFor(_displayOffsets));
            }

    #if UNITY_EDITOR
            private void OnDrawGizmosSelected()
            {
                if (axes == null || axes.Length == 0) return;

                bool playing = Application.isPlaying && IsSpawned;
                Vector3 origin = playing ? _origin : transform.position;

                Vector3 minPos = origin, maxPos = origin, curPos = origin;
                Vector3[] dirs = new Vector3[axes.Length];

                for (int a = 0; a < axes.Length; a++)
                {
                    Vector3 axis = transform.TransformDirection(axes[a].localAxis).normalized;
                    dirs[a] = axis;
                    if (axis == Vector3.zero) continue;

                    float cur = playing ? (IsServer ? _offsets[a] : _displayOffsets[a]) : 0f;
                    minPos += axis * axes[a].minOffset;
                    maxPos += axis * axes[a].maxOffset;
                    curPos += axis * cur;

                    Color c = AxisColors[a % AxisColors.Length];
                    Gizmos.color = c;
                    Vector3 a0 = origin + axis * axes[a].minOffset;
                    Vector3 a1 = origin + axis * axes[a].maxOffset;
                    Gizmos.DrawLine(a0, a1);
                    Gizmos.DrawWireSphere(a0, 0.08f);
                    Gizmos.DrawWireSphere(a1, 0.08f);
                    DrawArrow(curPos, axis * 0.7f);
                    DrawArrow(curPos, -axis * 0.7f);
                }
                
                if (axes.Length >= 2)
                {
                    Vector3 p00 = origin + dirs[0] * axes[0].minOffset + dirs[1] * axes[1].minOffset;
                    Vector3 p10 = origin + dirs[0] * axes[0].maxOffset + dirs[1] * axes[1].minOffset;
                    Vector3 p11 = origin + dirs[0] * axes[0].maxOffset + dirs[1] * axes[1].maxOffset;
                    Vector3 p01 = origin + dirs[0] * axes[0].minOffset + dirs[1] * axes[1].maxOffset;
                    Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
                    Gizmos.DrawLine(p00, p10);
                    Gizmos.DrawLine(p10, p11);
                    Gizmos.DrawLine(p11, p01);
                    Gizmos.DrawLine(p01, p00);
                }

               
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
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(curPos, 0.06f);
                
                if (handles == null) return;
                foreach (var h in handles)
                {
                    if (h == null) continue;
                    Gizmos.color = AxisColors[Mathf.Abs(h.Axis) % AxisColors.Length];
                    Gizmos.DrawWireSphere(h.SnapPoint.position, 0.15f);
                    Gizmos.DrawLine(h.SnapPoint.position, h.SnapPoint.position + h.SnapPoint.forward * 0.4f);
                }
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

            private void FixedUpdate()
            {
                if (!IsSpawned) return;
                float dt = Time.fixedDeltaTime;
                if (IsServer) ServerTick(dt);
                else ClientFollow(dt);
                LocalTick(dt);
            }
        }
    }