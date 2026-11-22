using MyPrint;
using Unity.Netcode;
using UnityEngine;

public class PhotoView : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer _spRender;
    [SerializeField] private Vector2 _targetSize = new Vector2(1f, 1f);    
    
    private byte[] _currentPhotoData;
    private Texture2D _currentTexture;
    private Sprite _currentSprite;
    
    public void SendPhoto(byte[] data)
    {
        if (!IsSpawned)
            return;

        UploadPhotoRpc(data);
    }

    [Rpc(SendTo.Server)]
    private void UploadPhotoRpc(byte[] data)
    {
        BroadcastPhotoRpc(data);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastPhotoRpc(byte[] data)
    {
        DisplayPhoto(data);
    }

    public void DisplayPhoto(byte[] photoData)
    {
        if (_currentSprite != null)
            Destroy(_currentSprite);

        if (_currentTexture != null)
            Destroy(_currentTexture);

        _currentTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!_currentTexture.LoadImage(photoData))
        {
            Destroy(_currentTexture);
            _currentTexture = null;
            return;
        }

        _currentSprite = Sprite.Create(_currentTexture, new Rect(0, 0, _currentTexture.width, _currentTexture.height), new Vector2(0.5f, 0.5f));
        
        _currentPhotoData = (byte[])photoData.Clone();

        _spRender.sprite = _currentSprite;

        float scale = _targetSize.x / _currentSprite.bounds.size.x;
        _spRender.transform.localScale = Vector3.one * scale;
    }
    
    public byte[] GetPhotoData()
    {
        return _currentPhotoData;
    }

    public void SetPhotoData(byte[] data)
    {
        if (data == null || data.Length == 0)
            return;

        _currentPhotoData = (byte[])data.Clone();
        DisplayPhoto(_currentPhotoData);
    }
}
