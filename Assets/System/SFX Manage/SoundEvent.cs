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
    public string SoundName;
    public Vector3 Position;
    public float Volume;
    public float Pitch;
    public bool Is2D; // true pour UI
    public bool Shared; // true si son envoyé au autres joueurs 
}
