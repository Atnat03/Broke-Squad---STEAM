using UnityEngine;

namespace Gameplay.LD
{
    public enum DoorMode { Toggle, Open, Close }

    public enum FaceAxis { Forward, Right, Up }

    public class OpenDoorEffect : LDEffectModule
    {
        [SerializeField] private DoorMode _mode;
        [SerializeField] private FaceAxis _faceAxis = FaceAxis.Right;
        [SerializeField] private bool _invertSide;

        private Transform Ref => Context.Element.transform;

        private bool IsOpen => Context.HasFlag(LDFlag.OpenFront) || Context.HasFlag(LDFlag.OpenBack);

        private Vector3 FaceNormal => _faceAxis switch
        {
            FaceAxis.Right => Ref.right,
            FaceAxis.Up => Ref.up,
            _ => Ref.forward
        };

        public override void Activate(LDActivation activation)
        {
            Debug.Log($"[LD] Porte activation");
            switch (_mode)
            {
                case DoorMode.Toggle:
                    Toggle(activation);
                    break;
                case DoorMode.Open:
                    Open(activation);
                    break;
                case DoorMode.Close:
                    Close();
                    break;
            }
        }

        private void Toggle(LDActivation activation)
        {
            if (IsOpen) Close();
            else Open(activation);
        }

        private void Close()
        {
            Context.SetFlag(LDFlag.OpenFront, false);
            Context.SetFlag(LDFlag.OpenBack, false);
        }

        private void Open(LDActivation activation)
        {
            int side = GetSide(activation);
            Debug.Log($"[LD] Porte : côté {side}");
            if(side > 0)
            {
                Context.SetFlag(LDFlag.OpenFront, true);
                Context.SetFlag(LDFlag.OpenBack, false);
            }
            else
            {
                Context.SetFlag(LDFlag.OpenBack, true);
                Context.SetFlag(LDFlag.OpenFront, false);
            }
        }

        private int GetSide(LDActivation activation)
        {
            if (!activation.TryGetInstigator(out var player)) return 1;

            Vector3 toPlayer = player.transform.position - Ref.position;
            toPlayer.y = 0f;

            int side = Vector3.Dot(FaceNormal, toPlayer) >= 0f ? 1 : -1;
            return _invertSide ? -side : side;
        }
    }
}