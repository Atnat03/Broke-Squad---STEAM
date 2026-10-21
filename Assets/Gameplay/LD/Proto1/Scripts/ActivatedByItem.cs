using Gameplay.Items;
using MyPrint;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.LD.Proto1.Scripts
{
    public class ActivatedByItem : NetworkBehaviour
    {
        [SerializeField] private OnlineInteractable _interactable;

        public void TriggerActivation()
        {
            _interactable.OnInteractParent();
        }
    }
}