using Gameplay.Items;
using UnityEngine;

public class PhotoModuleView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Vector2 targetSize = Vector2.one;

    private PhotoModule _module;
    private int _lastVersion = -1;

    private Texture2D _texture;
    private Sprite _sprite;

    private void Start()
    {
        ItemCore core = GetComponentInParent<ItemCore>();

        if (core == null || core.Instance == null)
            return;

        _module = core.Instance.GetModule(typeof(PhotoModule))
            as PhotoModule;

        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_module == null || _module.Version == _lastVersion)
            return;

        byte[] data = _module.GetByteState();

        if (data.Length == 0)
            return;

        Texture2D texture = new Texture2D(2, 2);

        if (!texture.LoadImage(data))
        {
            Destroy(texture);
            return;
        }

        Cleanup();

        _texture = texture;

        _sprite = Sprite.Create(
            _texture,
            new Rect(0, 0, _texture.width, _texture.height),
            new Vector2(0.5f, 0.5f)
        );

        _renderer.sprite = _sprite;

        float ratio = (float)_texture.width / _texture.height;

        _renderer.size = ratio > 1
            ? new Vector2(targetSize.x, targetSize.x / ratio)
            : new Vector2(targetSize.y * ratio, targetSize.y);

        _lastVersion = _module.Version;
    }

    private void Cleanup()
    {
        if (_sprite != null)
            Destroy(_sprite);

        if (_texture != null)
            Destroy(_texture);

        _sprite = null;
        _texture = null;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}