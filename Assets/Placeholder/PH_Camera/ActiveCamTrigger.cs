using System;
using UnityEngine;

public class ActiveCamTrigger : MonoBehaviour
{
    [SerializeField] private GameObject _camera;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _camera.SetActive(true);
        }
    }
}
