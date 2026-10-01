using System;
using UnityEngine;

public class ActiveCamTrigger : MonoBehaviour
{
    public GameObject camera;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            camera.SetActive(true);
        }
    }
}
