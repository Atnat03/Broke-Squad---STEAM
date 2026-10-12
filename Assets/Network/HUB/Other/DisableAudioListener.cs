using System;
using Bus;
using UnityEngine;

namespace Network.HUB
{
    public class DisableAudioListener : MonoBusListener
    {
        private AudioListener _audioListener;

        private void Awake()
        {
            _audioListener = GetComponent<AudioListener>();
            
            ListenToEvent<OnStartCreateGame_EVENT>(DisableListener);
        }

        private void DisableListener(OnStartCreateGame_EVENT obj)
        {
            _audioListener.enabled = false;
        }
    }
}