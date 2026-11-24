using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Gameplay.Controller
{
    public class JumpMetricsTool : MonoBehaviour
    {
        private enum Phase { Idle, RunUp, WaitingJump, InAir }

        private struct Result
        {
            public string Mode;
            public float Distance;   
            public float DeltaY;     
            public float Apex;       
            public float AirTime;
            public float Distance3D; 
            public float Forward;    
            public float Lateral;
        }

        [Header("Activation")]
        [SerializeField] private bool _toolEnabled = false;

        [Header("Keys")]
        [SerializeField] private Key _walkJumpKey = Key.F6;   
        [SerializeField] private Key _sprintJumpKey = Key.F7; 
        [SerializeField] private Key _clearKey = Key.F8; 

        [FormerlySerializedAs("_runUpTime")]
        [Header("Test")]
        [SerializeField] private float _runTimeBeforeJump = 0f;
        [SerializeField] private float _maxAirTimeExit = 10f;

        [Header("Display")]
        [SerializeField] private bool _showOnScreen = true;
        [SerializeField] private int _historySize = 10;

        [SerializeField] private PlayerController _controller;

        private Phase _phase = Phase.Idle;
        private bool _sprint;
        private bool _leftGround;
        private bool _subscribed;
        private float _timer;
        private Vector3 _startFeet;
        private float _apexY;
        private readonly List<Result> _history = new();
        private Vector3 _startForward;
        private int _settleTicks;
        private Vector3 _contactFeet;
        
        private void Awake()
        {
            if (_controller == null) _controller = GetComponent<PlayerController>();
        }

        private void OnDisable() => Abort(null);

        private void Update()
        {
            if (!_toolEnabled)
            {
                if (_phase != Phase.Idle) Abort("Tool disabled");
                return;
            }
            if (_controller == null || !_controller.IsSpawned || !_controller.IsOwner) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            bool walkPressed = kb[_walkJumpKey].wasPressedThisFrame;
            bool sprintPressed = kb[_sprintJumpKey].wasPressedThisFrame;

            if (_phase == Phase.Idle)
            {
                if (walkPressed) StartTest(false);
                else if (sprintPressed) StartTest(true);
            }
            else if (walkPressed || sprintPressed)
            {
                Abort("Test cancelled");
            }

            if (kb[_clearKey].wasPressedThisFrame) _history.Clear();
        }

        private void FixedUpdate()
        {
            if (_phase == Phase.Idle) return;
            _timer += Time.fixedDeltaTime;

            switch (_phase)
            {
                case Phase.RunUp:
                    if (_timer >= _runTimeBeforeJump) RequestJump();
                    break;

                case Phase.WaitingJump:
                    if (_timer > 0.5f) Abort("The jump never triggered");
                    break;

                case Phase.InAir:
                    Vector3 feet = _controller.Body.FeetPosition;
                    _apexY = Mathf.Max(_apexY, feet.y);

                    if (!_controller.IsGrounded)
                    {
                        _leftGround = true;
                        _settleTicks = 0;
                    }
                    else if (_leftGround)
                    {
                        if (_settleTicks == 0)
                        {
                            _contactFeet = feet;               
                            _controller.SetForcedInput(null, false); 
                        }

                        _settleTicks++;
                        if (_settleTicks >= 3)
                        {
                            Finish(new Vector3(_contactFeet.x, feet.y, _contactFeet.z));
                            _settleTicks = 0;
                            break;
                        }
                    }

                    if (_timer > _maxAirTimeExit) Abort("Max air time reached");
                    break;
            }
        }

        private void StartTest(bool sprint)
        {
            if (!_controller.IsGrounded) return;

            _sprint = sprint;
            _timer = 0f;
            _controller.SetForcedInput(Vector2.up, sprint);

            if (_runTimeBeforeJump > 0f) _phase = Phase.RunUp;
            else RequestJump();
        }

        private void RequestJump()
        {
            _timer = 0f;
            _phase = Phase.WaitingJump;

            if (!_subscribed)
            {
                _controller.Motor.OnJump += OnJumped;
                _subscribed = true;
            }
            _controller.Motor.BufferJump();
        }
        
        private void OnJumped()
        {
            _startFeet = _controller.Body.FeetPosition;
            _apexY = _startFeet.y;
            _leftGround = true;
            _timer = 0f;
            _phase = Phase.InAir;
            _startForward = _controller.transform.forward;
            _startForward.y = 0f;
            _startForward.Normalize();
            Unsubscribe();
        }

        private void Finish(Vector3 landingFeet)
        {
            _controller.SetForcedInput(null, false);

            Vector3 full = landingFeet - _startFeet;
            Vector3 d = new Vector3(full.x, 0f, full.z);
            Vector3 right = Vector3.Cross(Vector3.up, _startForward);

            var r = new Result
            {
                Mode = _sprint ? "Sprint" : "Walk",
                Distance = d.magnitude,
                Distance3D = full.magnitude,
                Forward = Vector3.Dot(d, _startForward),
                Lateral = Vector3.Dot(d, right),
                DeltaY = landingFeet.y - _startFeet.y,
                Apex = _apexY - _startFeet.y,
                AirTime = _timer
            };

            _history.Add(r);
            if (_history.Count > _historySize) _history.RemoveAt(0);

            _phase = Phase.Idle;
        }

        private void Abort(string reason)
        {
            Unsubscribe();
            if (_controller != null) _controller.SetForcedInput(null, false);
            if (_phase != Phase.Idle && reason != null) Debug.LogWarning("[JumpMetrics] " + reason);
            _phase = Phase.Idle;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_controller != null && _controller.Motor != null) _controller.Motor.OnJump -= OnJumped;
            _subscribed = false;
        }
        
        private GUIStyle _style;
        private void OnGUI()
        {
            if (!_toolEnabled || !_showOnScreen) return;

            if (_style == null) _style = new GUIStyle(GUI.skin.label) { richText = false };
            _style.fontSize = 22;
            _style.alignment = TextAnchor.UpperRight;
            _style.normal.textColor = Color.white;

            var sb = new StringBuilder();
            sb.AppendLine($"JUMP METRICS ({_phase})");
            sb.AppendLine($"{_walkJumpKey}: walk+jump | {_sprintJumpKey}: sprint+jump | {_clearKey}: clear");
            for (int i = _history.Count - 1; i >= 0; i--)
            {
                var r = _history[i];
                sb.AppendLine($"{r.Mode}: H {r.Distance:F2} | fwd {r.Forward:F2} | side {r.Lateral:+0.00;-0.00;0.00} | 3D {r.Distance3D:F2} | dY {r.DeltaY:+0.00;-0.00;0.00} | {r.AirTime:F2}s");
            }

            string text = sb.ToString();
            Vector2 size = _style.CalcSize(new GUIContent(text));
            var rect = new Rect(Screen.width - size.x - 15, 15, size.x, size.y);
            
            Color old = _style.normal.textColor;
            _style.normal.textColor = Color.black;
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, _style);
            _style.normal.textColor = old;

            GUI.Label(rect, text, _style);
        }
    }
}