using UnityEngine;

namespace Network.Connections
{
    public class PlayerLocalData : MonoBehaviour
    {
        public static PlayerLocalData instance; 
        
        public string PlayerName = "";
        public Color PlayerColor = Color.white;
        
        void Awake()
        {
            if(instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}