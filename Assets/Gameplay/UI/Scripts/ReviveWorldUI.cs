using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Gameplay.Controller
{
    public class ReviveWorldUI : MonoBehaviour
    {
        [SerializeField] private PlayerRevive revive;
        [SerializeField] private GameObject root;  
        [SerializeField] private Image fillImage;   
        [SerializeField] private float smoothSpeed = 10f;

        [Header("Placement")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.3f, 0f);

        private float _displayed;
        private Camera _cam;

        private void LateUpdate()
        {
            bool visible = revive.IsSpawned && !revive.IsOwner && revive.Health != null && revive.Health.IsDowned;

            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible)
            {
                _displayed = 0f;
                return;
            }

            _displayed = Mathf.Lerp(_displayed, revive.ReviveProgress,
                1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
            fillImage.fillAmount = _displayed;

            if (_cam == null) _cam = Camera.main;
            root.transform.position = revive.transform.position + worldOffset;
            if (_cam != null)
                root.transform.rotation = Quaternion.LookRotation(root.transform.position - _cam.transform.position);
        }
    }
}