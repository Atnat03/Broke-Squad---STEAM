using System;
using System.Linq;
using TMPro;
using Unity.Multiplayer.PlayMode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network.HUB
{
    public class SceneAutoConnectionUI : MonoBehaviour
    {
        [SerializeField] private SceneAutoConnection _sceneAutoService;
        
        [Header("List Scene")]
        [SerializeField] private SceneListElement _sceneListPrefab;
        [SerializeField] private Transform _sceneListParent;
        [SerializeField] private TextMeshProUGUI _selectedSceneName;
        
        private void OnEnable()
        {
            _sceneAutoService.OnSelectedSceneChanged += ChangeSceneName;
        }
        
        private void OnDisable()
        {
            _sceneAutoService.OnSelectedSceneChanged -= ChangeSceneName;
        }
        
        public void Start()
        {
            int n = SceneManager.sceneCountInBuildSettings;
        
            for (int i = 1; i < n; i++)
            {
                SceneListElement item = Instantiate(_sceneListPrefab, _sceneListParent);
            
                item.SetScene(i, _sceneAutoService);
            }
        }

        private void ChangeSceneName(string newName)
        {
            _selectedSceneName.text = newName;
        }
    }
}