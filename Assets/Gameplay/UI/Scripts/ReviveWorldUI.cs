using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Controller
{
    public class ReviveWorldUI : MonoBehaviour
    {
        [SerializeField] private PlayerResurrection resurrection;
        [SerializeField] private GameObject root;  
        [SerializeField] private Image fillImage;   
        [SerializeField] private float smoothSpeed = 10f;

        [Header("Placement")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.3f, 0f);

        private float _displayed;
        private Camera _cam;

        private void LateUpdate()
        {
            bool visible = resurrection.IsSpawned && !resurrection.IsOwner && resurrection.IsDowned.Value;

            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible)
            {
                _displayed = 0f;
                return;
            }

            _displayed = Mathf.Lerp(_displayed, resurrection.ReviveProgress.Value,
                1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
            fillImage.fillAmount = _displayed;

            if (_cam == null) _cam = Camera.main;
            
            root.transform.position = resurrection.transform.position + worldOffset;

            if (_cam != null)
                root.transform.rotation = Quaternion.LookRotation(root.transform.position - _cam.transform.position);
        }
    }
}