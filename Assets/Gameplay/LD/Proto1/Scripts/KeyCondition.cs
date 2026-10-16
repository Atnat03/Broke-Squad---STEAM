using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD.Proto1.Scripts
{
    [RequireComponent(typeof(Door))]
    public class KeyCondition : NetworkBehaviour
    {
        [Header("Key")]
        [SerializeField] private int _keyId = 0;
        [SerializeField] private bool _canBeOpenWithoutKey = false;
        private bool _alreadyOpened = false;
        
        public bool TryOpen(Vector2Int range)
        {
            if (!IsServer) return false;
            
            if (_alreadyOpened) return true;
            
            if(_canBeOpenWithoutKey) return true;
            
            if(range == new Vector2Int(-1, -1)) return true;
            
            if (range.x <= _keyId && _keyId <= range.y)
            {
                _alreadyOpened = true;
                return true;
            }

            return false;
        }
    }
}