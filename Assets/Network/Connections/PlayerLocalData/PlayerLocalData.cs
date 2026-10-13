using UnityEngine;

namespace Network.Connections
{
    public class PlayerLocalData : MonoBehaviour
    {
        public static PlayerLocalData instance;

        public Color[] PossibleColor;
        
        public string PlayerName = "";
        public int PlayerColor = 0;
        
        void Awake()
        {
            if(instance == null)
            {
                instance = this;

                PlayerName = "Player :" + Random.Range(0, 10000);
                
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}