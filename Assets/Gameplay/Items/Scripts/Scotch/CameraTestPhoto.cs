using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items.Scripts.Scotch
{
    public class CameraTestPhoto : MonoBehaviour
    {
        public Camera photoCamera;
        
        private Texture2D _screenCapture;
        private RenderTexture _renderTexture;
        private Sprite _photoSprite;
        public PhotoView _image;

        private void Start()
        {
            _renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);

            _screenCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);

            photoCamera.targetTexture = _renderTexture;
            photoCamera.enabled = false;
        }


        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                StartCoroutine(CapturePhoto());
            }
        }
        
        IEnumerator CapturePhoto()
        {
            yield return new WaitForEndOfFrame();

            Camera mainCamera = Camera.main;
            
            // Synchronise la caméra photo avec la caméra principale
            photoCamera.transform.position = mainCamera.transform.position;
            photoCamera.transform.rotation = mainCamera.transform.rotation;

            photoCamera.fieldOfView = mainCamera.fieldOfView;
            photoCamera.aspect = mainCamera.aspect;
            photoCamera.nearClipPlane = mainCamera.nearClipPlane;
            photoCamera.farClipPlane = mainCamera.farClipPlane;

            // Capture uniquement les layers autorisés par son Culling Mask
            photoCamera.Render();

            RenderTexture.active = _renderTexture;

            _screenCapture.ReadPixels(
                new Rect(0, 0, _renderTexture.width, _renderTexture.height),
                0,
                0
            );

            _screenCapture.Apply();

            RenderTexture.active = null;

            ShowPhoto();
        }

        private void ShowPhoto()
        {
            if (_photoSprite != null)
            {
                Destroy(_photoSprite);
            }

            _photoSprite = Sprite.Create(
                _screenCapture,
                new Rect(0, 0, _screenCapture.width, _screenCapture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            byte[] photoData = CreateNetworkPhoto(_screenCapture, 320, 60);
            _image.SendPhoto(photoData);
        }
        
        private byte[] CreateNetworkPhoto(Texture2D source, int maxWidth, int quality)
        {
            float ratio = (float)source.height / source.width;

            int width = Mathf.Min(source.width, maxWidth);
            int height = Mathf.RoundToInt(width * ratio);

            RenderTexture rt = RenderTexture.GetTemporary(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32
            );

            RenderTexture previous = RenderTexture.active;

            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            Texture2D resizedTexture = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false
            );

            resizedTexture.ReadPixels(
                new Rect(0, 0, width, height),
                0,
                0
            );

            resizedTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            byte[] jpgData = resizedTexture.EncodeToJPG(quality);

            Destroy(resizedTexture);

            Debug.Log($"Photo réseau : {width}x{height} | {jpgData.Length / 1024f:F1} KB");

            return jpgData;
        }
    }
}