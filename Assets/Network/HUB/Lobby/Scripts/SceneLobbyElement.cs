using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network.HUB
{
    public class SceneLobbyElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _sceneNameText;
        private int _sceneId;
        private string _sceneName = string.Empty;
        private LobbyManager _connect;
        
        public void SetScene(int sceneId, LobbyManager connect)
        {
            _sceneId = sceneId;
            _connect = connect;

            string path = SceneUtility.GetScenePathByBuildIndex(_sceneId);
            _sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

            _sceneNameText.text = _sceneName;
        }

        public void SetNewMap()
        {
            _connect.SetMapButton(_sceneName);
        }
    }
}
