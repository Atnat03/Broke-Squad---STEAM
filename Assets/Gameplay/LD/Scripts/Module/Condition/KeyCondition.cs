using System.Security.Cryptography.X509Certificates;
using Gameplay.Items;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    public class KeyCondition : LDConditionModule
    {
        [SerializeField] private int _idKey;
        [SerializeField] private bool _canOpenWithoutKey;
        private bool _isAlreadyOpened;
        
        public override bool CanActivate(LDActivation activation)
        {
            if (_canOpenWithoutKey)
                return true;
            
            if (_isAlreadyOpened)
                return true;
            
            ABPrint.Print("Can Activate 0", ABColor.Orange);

            NetworkObject playerNO = NetworkManager.Singleton.ConnectedClients[activation.InstigatorClientId].PlayerObject;

            PlayerInventory playerInventory = playerNO.GetComponent<PlayerInventory>();
            
            ABPrint.Print("Can Activate 1", ABColor.Orange);
            
            if (playerInventory)
            {
                ABPrint.Print("Can Activate 2", ABColor.Orange);
                
                ItemInstance item = playerInventory.GetCurrentItemInHand();

                foreach (IFirstAction first in item.FirstActionList)
                {
                    ABPrint.Print("Can Activate 3", ABColor.Orange);
                    
                    if (first is OpenDoorModule)
                    {
                        ABPrint.Print("Can Activate 4", ABColor.Orange);
                        
                        OpenDoorModule module = first as OpenDoorModule;

                        bool result = CheckDoor(module.Range);
                        
                        if(result)
                            _isAlreadyOpened = true;

                        return result;
                    }
                }
            }

            return false;
        }

        private bool CheckDoor(Vector2Int moduleRange)
        {
            ABPrint.Print("Can Activate 5", ABColor.Orange);
            
            if(moduleRange == new Vector2Int(-1, -1))
                return true;
            
            ABPrint.Print("Can Activate 6", ABColor.Orange);
            
            return moduleRange.x <= _idKey && moduleRange.y >= _idKey;
        }
    }
}