using System.Collections;
using Bus;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Other.Sound.Music
{
    public class MusicManager : MonoBusListener
    {
        [Header("Music")]
        [SerializeField] private AudioClip _normalMusic;
        [SerializeField] private AudioClip _dangerousMusic;
        [SerializeField] private AudioSource _audioSource;

        private void Awake()
        {
           ListenToEvent<OnGrabGoal_EVENT>(ChangeMusicToDangerous);
            ListenToEvent<OnDropGoal_EVENT>(ChangeMusicToNormal);
        }

        private void Start()
        {
            PlayTheMusic(_normalMusic);
        }

        private void ChangeMusicToDangerous(OnGrabGoal_EVENT data)
        {
            PlayTheMusic(_dangerousMusic);
        }

        private void ChangeMusicToNormal(OnDropGoal_EVENT data)
        {
            PlayTheMusic(_normalMusic);
        }

        private void PlayTheMusic(AudioClip clip)
        {
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