using System;
using TMPro;
using UnityEngine;

namespace Gameplay.UI.Scripts
{
    public class PlayerPseudoSetUp : MonoBehaviour
    {
        [SerializeField] private PlayerSetUpBridge _bridge;
        [SerializeField] private TextMeshProUGUI _playerName;
        
        void Start()
        {
            _bridge.PlayerController.OnPseudoIsSetUp += SetUpPseudo;
        }

        private void OnDisable()
        {
            _bridge.PlayerController.OnPseudoIsSetUp -= SetUpPseudo;
        }
        
        private void SetUpPseudo(string nom)
        {
            _playerName.text = nom;
        }

    }
}