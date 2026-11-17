using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Network.Communication.Pings
{
    public class SpecialPing : NetworkBehaviour
    {
        [SerializeField] private Sprite _specialPingSprite;
        [SerializeField] private Ping _pingPrefab;
        
        public void AddSpecialPing(Vector3 pos, int colorId, float duration)
        {
            if (!IsServer)
            {
                AddSpecialPingServerRpc(pos, colorId, duration);
                return;
            }

            AddSpecialPingClientRpc(pos, colorId, duration);
        }
        
        [Rpc(SendTo.Server)]
        private void AddSpecialPingServerRpc(Vector3 pos, int colorId, float duration) => AddSpecialPing(pos, colorId, duration);

        [Rpc(SendTo.Everyone)]
        private void AddSpecialPingClientRpc(Vector3 pos, int colorId, float duration)
        {
            Ping specialPingInstance = Instantiate(_pingPrefab, pos, Quaternion.identity);
            specialPingInstance.SetSprites(colorId, duration, _specialPingSprite);
        }
    }
}