using UnityEngine;

namespace Network.Connections
{
    public class PlayerLocalData : MonoBehaviour
    {
        public static PlayerLocalData instance;

        public Color[] PossibleColor;
        
        public string PlayerName = "";
        public int PlayerColor = 0;
        
        [SerializeField] private string[] _potentialPseudo;
        
        void Awake()
        {
            if(instance == null)
            {
                instance = this;

                PlayerName = _potentialPseudo[Random.Range(0, _potentialPseudo.Length)];
                
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}