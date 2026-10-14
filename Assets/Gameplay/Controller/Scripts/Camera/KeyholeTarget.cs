using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Controller
{
    public class KeyholeTarget : MonoBehaviour, IInteractable
    {
        [SerializeField] private Outline _outline;
        
        public Transform viewPoint;
        public Transform standPoint;

        [Header("Limits")]
        public float maxYaw = 25f;
        public float maxPitch = 20f;
        public float fov = 40f;
        public float blendTime = 0.35f;
        
        [Range(0f, 1f)] public float approachAmount = 0.85f;

        private KeyholeView _view;

        public Outline outline => _outline;

        public float BaseYaw => viewPoint.eulerAngles.y;

        public float BasePitch
        {
            get
            {
                float p = viewPoint.eulerAngles.x;
                return p > 180f ? p - 360f : p;
            }
        }

        public bool InUse => _view != null && !_view.IsFinished;

        public void SetOutline(bool state)
        {
            if (_outline != null) _outline.enabled = state;
        }

        public void Interact(ulong clientId)
        {
            var playerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (playerObject == null || !playerObject.TryGetComponent(out PlayerController player)) return;

            Begin(player);
        }

        public void Begin(PlayerController player)
        {
            if (InUse || viewPoint == null || player.IsDown) return;

            _view = new KeyholeView(this, () =>
            {
                if (standPoint != null) player.SnapTo(standPoint.position, BaseYaw);
            });
            player.playerCamera.SetOverride(_view);
        }
    }
}