using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class PolaroidCameraModule : ActionModule, IFirstAction, IServerAction
    {
        [SerializeField] private PhotoModuleOnGroundView _pictureToDropPrefab;
        [SerializeField] private Vector2Int _photoSize = new Vector2Int(640, 640);
        
        //Captured texture
        private Texture2D _screenCapture;
        private RenderTexture _renderTexture;
        private Camera _photoCamera;
        
        public ICondition Condition => ConditionParent;

        public void StartFirstAction()
        {
            _renderTexture = new RenderTexture(_photoSize.x, _photoSize.y, 24, RenderTextureFormat.ARGB32);
            _screenCapture = new Texture2D(_photoSize.x, _photoSize.y, TextureFormat.RGBA32, false);

            _photoCamera = Context.Inventory.PhotoCamera;
            _photoCamera.targetTexture = _renderTexture;
            _photoCamera.enabled = false;
            
            Context.StartCoroutine(CapturePhoto());
        }

         IEnumerator CapturePhoto()
        {
            yield return new WaitForEndOfFrame();

            Camera mainCamera = Context.Camera;
            
            // Synchronise la caméra photo avec la caméra principale
            _photoCamera.transform.position = mainCamera.transform.position;
            _photoCamera.transform.rotation = mainCamera.transform.rotation;

            _photoCamera.fieldOfView = mainCamera.fieldOfView;
            _photoCamera.aspect = mainCamera.aspect;
            _photoCamera.nearClipPlane = mainCamera.nearClipPlane;
            _photoCamera.farClipPlane = mainCamera.farClipPlane;

            // Capture uniquement les layers autorisés par son Culling Mask
            _photoCamera.Render();

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
            byte[] photoData = CreateNetworkPhoto(_screenCapture, 320, 60);
            
            Context.SendToServer(this, new OnModuleDoAction_EVENT
            {
                BytesData = photoData,
                Position = Context.Core.transform.position,
                Rotation = Context.Core.transform.rotation,
            });
        }
        
        private byte[] CreateNetworkPhoto(Texture2D source, int size, int quality)
        {
            int cropSize = Mathf.Min(source.width, source.height);

            int startX = (source.width - cropSize) / 2;
            int startY = (source.height - cropSize) / 2;

            Color[] pixels = source.GetPixels(
                startX,
                startY,
                cropSize,
                cropSize
            );

            Texture2D squareTexture = new Texture2D(
                cropSize,
                cropSize,
                TextureFormat.RGB24,
                false
            );

            squareTexture.SetPixels(pixels);
            squareTexture.Apply();

            RenderTexture rt = RenderTexture.GetTemporary(
                size, size, 0, RenderTextureFormat.ARGB32
            );

            RenderTexture previous = RenderTexture.active;

            Graphics.Blit(squareTexture, rt);
            RenderTexture.active = rt;

            Texture2D resizedTexture = new Texture2D(
                size, size, TextureFormat.RGB24, false
            );

            resizedTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            resizedTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            byte[] jpgData = resizedTexture.EncodeToJPG(quality); ;

            Debug.Log($"Photo réseau : {size}x{size} | {jpgData.Length / 1024f:F1} KB");

            return jpgData;
        }

        public void StopFirstAction()
        {
        }

        public void OnNotInHand()
        {
        }

        public void OnThrow()
        {
        }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            NetworkObject spawned = Context.SpawnGameObject(_pictureToDropPrefab.gameObject, data.Position, data.Rotation);

            spawned.transform.rotation *=  Quaternion.Euler(0f, 180f, 0);
            
            if (spawned.TryGetComponent(out ItemPickup pickup))
            {
                PhotoModule module = pickup.Instance.GetModule(typeof(PhotoModule)) as PhotoModule;

                module?.SetPhotoData(data.BytesData);

                pickup.SyncByteModulesToClients();
            }
        }
    }
}