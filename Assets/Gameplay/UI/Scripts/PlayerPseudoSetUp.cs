using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gameplay.UI.Scripts
{
    public class PlayerPseudoSetUp : MonoBehaviour
    {
        [SerializeField] private PlayerSetUpBridge _bridge;
        [SerializeField] private TextMeshProUGUI _playerName;
        
        
        void Start()
        {
            _bridge.PlayerController.OnPseudoIsSetUp += SetUpPseudo;
            _bridge.PlayerOutlineToggle.OnSeeThroughChanged += SetSeeThrough;
            SetSeeThrough(_bridge.PlayerOutlineToggle.IsSeeThrough);
        }

        private void OnDisable()
        {
            _bridge.PlayerController.OnPseudoIsSetUp -= SetUpPseudo;
            _bridge.PlayerOutlineToggle.OnSeeThroughChanged -= SetSeeThrough;
        }
        
        private void SetUpPseudo(string nom)
        {
            _playerName.text = nom;
        }
        
        private void SetSeeThrough(bool seeThrough)
        {
            var mat = _playerName.fontMaterial; 
            mat.SetInt("unity_GUIZTestMode", (int)(seeThrough ? CompareFunction.Always : CompareFunction.LessEqual));
            mat.renderQueue = seeThrough ? 4000 : -1; 
        }

    }
}