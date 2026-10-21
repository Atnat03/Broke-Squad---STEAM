using System;
using Gameplay.Controller;
using Gameplay.Controller.Minimap;
using MyPrint;
using Network.Connections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerMinimap : NetworkBehaviour
{
    [SerializeField] private PlayerController _controller;
    [SerializeField] private Camera _camMapPrefab;
    
    [Header("Sprite")]
    [SerializeField] private SpriteRenderer _playerArrowSprite;
    [SerializeField] private Color[] _possibleColor;
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Camera c = Instantiate(_camMapPrefab,
                transform.position + _camMapPrefab.transform.position,
                _camMapPrefab.transform.rotation);
            
            c.AddComponent<FreezeRotationMinimap>().SetTarget(transform);
        }
    }

    private void OnEnable()
    {
        _controller.OnPersonalisationChanged += SetArrow;
    }

    private void SetArrow()
    {
        _playerArrowSprite.color = _possibleColor[_controller.PlayerColorId];
    }
}
