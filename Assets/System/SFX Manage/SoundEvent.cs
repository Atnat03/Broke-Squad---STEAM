using UnityEngine;

// Liste des events de son
/*
 * public struct SoundEvent
 * {
 *      public int exemple;
 * }
 */

public struct PlaySoundEvent
{
    public string soundName;
    public Vector3 position;
    public float volume;
    public float pitch;
    public bool is2D; // true pour UI
    public bool shared; // true si son envoyé au autres joueurs 
}

public struct SfxSettingsChangedEvent
{
    public float volume;
    public bool muted;
}