using UnityEngine;

namespace ConstructionSiteEssentials.NightShift
{
    public sealed class NightShiftJob : MonoBehaviour
    {
        public Camera view;
        public CarryAndPlace reel, stand;
        public SwitchableWorkLight workLight;
        public Transform reelBay, lightBay;
        public GameObject reelMark, lightMark;
        public enum Task { TakeReel, PlaceReel, TakeStand, PlaceStand, PowerOn, Finished }
        public Task Current { get; private set; }
        public string Notice { get; private set; }
        float noticeUntil;

        public bool Carrying => Current == Task.PlaceReel || Current == Task.PlaceStand;
        public CarryAndPlace Item => Current <= Task.PlaceReel ? reel : stand;
        public Transform Socket => Current <= Task.PlaceReel ? reelBay : lightBay;

        void Start() { SetTask(Task.TakeReel); }
        void Update()
        {
            // Keep the complete work area in frame on narrower landscape windows.
            view.orthographicSize = Mathf.Max(2.15f, 3.68f / view.aspect);
            if (Time.unscaledTime >= noticeUntil) Notice = null;
        }

        void SetTask(Task next)
        {
            Current = next;
            reelMark.SetActive(next == Task.PlaceReel);
            lightMark.SetActive(next == Task.PlaceStand);
            Notice = null;
        }

        public void Restart()
        {
            reel.ResetItem();
            stand.ResetItem();
            workLight.SetPower(false);
            SetTask(Task.TakeReel);
        }

        public void CancelCarry()
        {
            if (!Carrying) return;
            Item.ResetItem();
            SetTask(Current == Task.PlaceReel ? Task.TakeReel : Task.TakeStand);
        }

        bool Ground(Vector2 screen, out Vector3 point)
        {
            Ray ray = view.ScreenPointToRay(screen);
            point = Vector3.zero;
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) return false;
            point = ray.GetPoint(distance);
            return true;
        }

        public void MovePointer(Vector2 screen)
        {
            if (!Carrying || !Ground(screen, out Vector3 point)) return;
            point.x = Mathf.Clamp(point.x, -2.9f, 2.45f);
            point.z = Mathf.Clamp(point.z, -1.9f, -0.28f);
            CarryAndPlace other = Item == reel ? stand : reel;
            Vector2 clearance = new Vector2(point.x - other.transform.position.x, point.z - other.transform.position.z);
            if (clearance.sqrMagnitude >= .68f * .68f) Item.CarryTo(point);
        }

        public void Activate(Vector2 screen, bool pickedLabel = false)
        {
            if (Current == Task.Finished) return;
            if (Carrying)
            {
                if (Ground(screen, out Vector3 point) && Item.TryPlace(point, Socket))
                    SetTask(Current == Task.PlaceReel ? Task.TakeStand : Task.PowerOn);
                else Hint("Place it inside the marked bay.");
                return;
            }
            if (!pickedLabel)
            {
                Ray ray = view.ScreenPointToRay(screen);
                if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide) || !hit.transform.IsChildOf(Item.transform))
                {
                    Hint(Current == Task.TakeReel ? "Select the cable reel first." : "Select the work light.");
                    return;
                }
            }
            if (Current == Task.PowerOn)
            {
                workLight.SetPower(true);
                SetTask(Task.Finished);
                return;
            }
            Item.PickUp();
            SetTask(Current == Task.TakeReel ? Task.PlaceReel : Task.PlaceStand);
        }

        void Hint(string message)
        {
            Notice = message;
            noticeUntil = Time.unscaledTime + 2.4f;
        }
    }
}

