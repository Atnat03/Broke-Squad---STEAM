using Bus;
using ScriptableObjectsDefinitions;
using System.Collections.Generic;
using UnityEngine;
using SFX_Manage;

public class SoundManager : MonoBusListener
{
    [SerializeField] private SoundsDataSO soundsData;
	[SerializeField] private int poolSize = 10;

	private readonly Dictionary<string, AudioClip> clips = new();
	private AudioSource[] pool;
	private int nextSource;
	private float masterVolume = 1f;
	private bool isMuted;

    private void Awake()
    {
		BuildClipDictionary();
    }

    private void BuildClipDictionary()
    {
        foreach (SoundData s in soundsData.sounds)
        {
            if (string.IsNullOrEmpty(s.soundName) || s.audioClip == null) continue;

            if (!clips.TryAdd(s.soundName, s.audioClip))
                Debug.LogWarning($"SoundManager: nom de son en double '{s.soundName}'");
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
            pool[i] = src;
        }
    }

    private void OnPlaySound(PlaySoundEvent e)
    {
        if (isMuted) return;

        if (!clips.TryGetValue(e.soundName, out AudioClip clip))
        {
            Debug.LogWarning($"SoundManager: son introuvable : '{e.soundName}'");
            return;
        }

        AudioSource src = pool[nextSource];
        nextSource = (nextSource + 1) % poolSize;

        src.transform.position = e.Position;
        src.clip = clip;
        src.volume = e.volume * masterVolume;
        src.pitch = e.pitch;
        src.transform.position = e.Position;
        src.Play();
    }

    private void OnSettingsChanged(SfxSettingsChangedEvent e)
    {
        masterVolume = e.volume;
        isMuted = e.muted;
    }
}