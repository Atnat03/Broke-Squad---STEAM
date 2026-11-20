using UnityEngine;
using Unity.Netcode;
using Gameplay.Items;

public class PhotoModuleOnGroundView : NetworkBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer _spRender;
    [SerializeField] private Vector2 targetSize = Vector2.one;

    private Texture2D _currentTexture;
    private Sprite _currentSprite;

    private PhotoModule _photoModule;
    private int _lastVersion = -1;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
            return;

        ItemPickup pickup = GetComponentInParent<ItemPickup>();

        if (pickup == null)
            return;

        PhotoModule module = pickup.Instance.GetModule(typeof(PhotoModule))
            as PhotoModule;

        if (module == null)
            return;

        byte[] data = module.GetByteState();

        if (data == null || data.Length == 0)
            return;

        BroadcastPhotoRpc(data);
    }
    
    private void Start()
    {
        ItemPickup pickup = GetComponentInParent<ItemPickup>();

        if (pickup != null && pickup.Instance != null)
        {
            _photoModule = pickup.Instance.GetModule(typeof(PhotoModule))
                as PhotoModule;
        }

        RefreshFromModule();
    }

    private void Update()
    {
        RefreshFromModule();
    }

    public void SendPhoto(byte[] pngData)
    {
        if (!IsSpawned || !IsServer || pngData == null || pngData.Length == 0)
            return;

        BroadcastPhotoRpc(pngData);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastPhotoRpc(byte[] pngData)
    {
        DisplayPhoto(pngData);
    }

    private void RefreshFromModule()
    {
        if (_photoModule == null)
        {
            ItemPickup pickup = GetComponentInParent<ItemPickup>();

            if (pickup != null && pickup.Instance != null)
            {
                _photoModule = pickup.Instance.GetModule(typeof(PhotoModule))
                    as PhotoModule;
            }
        }

        if (_photoModule == null || _photoModule.Version == _lastVersion)
            return;

        byte[] data = _photoModule.GetByteState();

        if (data.Length == 0)
            return;

        DisplayPhoto(data);
        _lastVersion = _photoModule.Version;
    }

    private void DisplayPhoto(byte[] pngData)
    {
        if (pngData == null || pngData.Length == 0)
            return;

        Texture2D texture = new Texture2D(2, 2);

        if (!texture.LoadImage(pngData))
        {
            Destroy(texture);
            return;
        }

        Cleanup();

        _currentTexture = texture;

        _currentSprite = Sprite.Create(
            _currentTexture,
            new Rect(0, 0, _currentTexture.width, _currentTexture.height),
            new Vector2(0.5f, 0.5f)
        );

        _spRender.sprite = _currentSprite;

        float ratio = (float)_currentTexture.width / _currentTexture.height;

        _spRender.size = ratio > 1
            ? new Vector2(targetSize.x, targetSize.x / ratio)
            : new Vector2(targetSize.y * ratio, targetSize.y);

    }

    private void Cleanup()
    {
        if (_currentSprite != null)
            Destroy(_currentSprite);

        if (_currentTexture != null)
            Destroy(_currentTexture);

        _currentSprite = null;
        _currentTexture = null;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}