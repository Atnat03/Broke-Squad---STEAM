using System;
using System.Collections;
using UnityEngine;

public class Ping : MonoBehaviour
{
    [SerializeField] private Color[] _possibleColor;
    [SerializeField] private SpriteRenderer _backgroundRenderer;
    [SerializeField] private SpriteRenderer _itemIconRenderer;
    
    Animator _pingAnimator;

    private void OnEnable()
    {
        _pingAnimator = GetComponent<Animator>();
    }

    public void SetSprites(int colorID, float lifeDuration, Sprite itemIcon = null)
    {
        _backgroundRenderer.color = _possibleColor[colorID];
        
        if(itemIcon != null && itemIcon != null)
            _itemIconRenderer.sprite = itemIcon;

        StartCoroutine(LifetimePing(lifeDuration));
    }

    private IEnumerator LifetimePing(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        if(_pingAnimator)
            _pingAnimator.SetTrigger("Destroy");
    }

    public void DestroyPing()
    {
        Destroy(gameObject);
    }
}
