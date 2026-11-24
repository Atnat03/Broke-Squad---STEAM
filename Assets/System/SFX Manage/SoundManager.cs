using Bus;
using ScriptableObjectsDefinitions;
using System.Collections.Generic;
using MyPrint;
using UnityEngine;
using SFX_Manage;
using UnityEngine.Audio;

namespace SFX_Manage
{
    public class SoundManager : MonoBusListener
    {
        [SerializeField] private List<SoundsDataSO> soundsData = new();
        [SerializeField] private int poolSize = 10;
        [SerializeField] private AudioMixerGroup sfxGroup;

        private readonly Dictionary<string, AudioClip> clips = new();
        private AudioSource[] pool;
        private int nextSource;

        private void Awake()
        {
            BuildClipDictionary();
            BuildPool();

            ListenToEvent<PlaySoundEvent>(OnPlaySound);
        }

        private void BuildClipDictionary()
        {
            foreach (SoundsDataSO data in soundsData)
            {
                foreach (SoundData s in data.sounds)
                {
                    if (string.IsNullOrEmpty(s.soundName) || s.audioClip == null) continue;

                    string key = data.GetKey(s);
                    if (!clips.TryAdd(key, s.audioClip))
                        Debug.LogWarning($"SoundManager: nom de son en double '{key}'");
                }
            }
        }

        private void BuildPool()
        {
            pool = new AudioSource[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                AudioSource src = new GameObject($"SFX_{i}").AddComponent<AudioSource>();
                src.transform.SetParent(transform);
                src.playOnAwake = false;
                src.spatialBlend = 1f; // 3D sound
                src.outputAudioMixerGroup = sfxGroup;
                pool[i] = src;
            }
        }

        private void OnPlaySound(PlaySoundEvent e)
        {
            
            if (!clips.TryGetValue(e.SoundName, out AudioClip clip))
            {
                Debug.LogWarning($"SoundManager: son introuvable : '{e.SoundName}'");
                return;
            }

            AudioSource src = pool[nextSource];
            nextSource = (nextSource + 1) % poolSize;

            src.transform.position = e.Position;
            src.clip = clip;
            src.volume = (e.Volume <= 0f ? 1f : e.Volume);
            src.pitch = e.Pitch <= 0f ? 1f : e.Pitch;
            src.spatialBlend = e.Is2D ? 0f : 1f;
            src.Play();
        }
        
    }
}