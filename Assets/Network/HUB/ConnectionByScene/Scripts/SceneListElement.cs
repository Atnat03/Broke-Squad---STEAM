using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network.HUB
{
    public class SceneListElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _sceneNameText;
        private int _sceneId;
        private SceneAutoConnection _connect;
        
        public void SetScene(int sceneId, SceneAutoConnection connect)
        {
            _sceneId = sceneId;
            _connect = connect;

            string sceneName = SceneUtility.GetScenePathByBuildIndex(_sceneId);
        
            _sceneNameText.text = System.IO.Path.GetFileNameWithoutExtension(sceneName);
        }

        public void SetConnection()
        {
            _connect.SelectScene(_sceneId);
            _connect.TryConnect();
        }
    }
}
