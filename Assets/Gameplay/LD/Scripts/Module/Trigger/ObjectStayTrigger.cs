using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public enum TriggerType
    {
        PLayer, 
        Object,
        Both
    }

    public class ObjectStayTrigger : LDTriggerModule
    {
        [Tooltip("Type de trigger : Player, Object ou les deux")]
        [SerializeField] private TriggerType _triggerType = TriggerType.PLayer;

        [Tooltip("Poid du joueur")]
        [SerializeField] private int _playerWeightValue = 10;

        [Tooltip("Poids minimum total des objets pour déclencher le trigger")]
        [SerializeField, Min(0f)] private int _weightToBeActivated = 0;

        [Tooltip("Le flag mis à jour : actif tant que la plaque est pressée.")]
        [SerializeField] private LDFlag _flag = LDFlag.Activated;


        protected override void OnInitialize()
        {
            if (!TryGetComponent<Collider>(out var collider))
                throw new Exception($"[LD] ObjectStayTrigger : {name} requires a Collider component");
            collider.isTrigger = true;
            if (!TryGetComponent<Rigidbody>(out _))
                gameObject.AddComponent<Rigidbody>().isKinematic = true;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!Context.IsServer) return;
            NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsPlayerObject) return;
            Raise(new LDActivation(netObj, netObj.transform.position));
        }
    }
}
