using System.Collections;
using Bus;
using Gameplay.IA;
using Gameplay.Items;
using MyPrint;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

namespace Gameplay.Other.Sound.Music
{
    public class MusicManager : NetworkBusListener
    {
        [Header("Music")]
        [SerializeField] private AudioClip[] _musicList;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioMixerGroup _musicGroup;

        private bool _canChangeMusic = true;
        
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;

            if (_audioSource != null && _musicGroup != null)
                _audioSource.outputAudioMixerGroup = _musicGroup;

            ListenToEvent<OnGrabGoal_EVENT>(ChangeMusicToDangerous);
            ListenToEvent<OnDropGoal_EVENT>(_ => ChangeMusicToNormal());
            ListenToEvent<OnLockDownStart_EVENT>(ChangeMusicToLockDown);
            ListenToEvent<OnLockDownEnd_EVENT>(_ => ChangeMusicToNormal());

            PlayTheMusicClientRpc(0);
        }

        private void ChangeMusicToDangerous(OnGrabGoal_EVENT data)
        {
            if (!_canChangeMusic) return;
            
            PlayTheMusicClientRpc(1);
            
            StartCoroutine(BufferingMusicChange());
        }

        private void ChangeMusicToNormal()
        {
            if (!_canChangeMusic) return;
            
            PlayTheMusicClientRpc(0);
            
            StartCoroutine(BufferingMusicChange());
        }

        private void ChangeMusicToLockDown(OnLockDownStart_EVENT data)
        {
            if (!_canChangeMusic) return;
            
            PlayTheMusicClientRpc(2);

            StartCoroutine(BufferingMusicChange());
        }

        private IEnumerator BufferingMusicChange()
        {
            _canChangeMusic = false;
            
            yield return new WaitForSeconds(0.2f);

            _canChangeMusic = true;
        }

        [Rpc(SendTo.Everyone)]
        private void PlayTheMusicClientRpc(int idClip)
        {
            if (_musicList == null ||
                idClip < 0 ||
                idClip >= _musicList.Length)
                return;

            AudioClip clip = _musicList[idClip];

            if (clip == null || _audioSource == null)
                return;

            if (_audioSource.clip == clip && _audioSource.isPlaying)
                return;

            _audioSource.clip = clip;
            _audioSource.loop = true;
            _audioSource.Play();
        }
    }
}