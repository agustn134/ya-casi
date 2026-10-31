using UnityEngine;
using TMPro;
public sealed class U2GameConfig : ScriptableObject
{
    public AudioClip soundtrack;
    public AudioClip menuMusic;
    public TMP_FontAsset font;
    public RuntimeAnimatorController[] studentControllers;
    public float roundSeconds = 135f;
    public float wrongLockerPenalty = 3f;
    public float repeatLockerPenalty = 5f;
    public int studentCount = 6;
    public string soundtrackCredit = "Pixel art — Brutaldesign (Pixabay Content License). pixabay.com/music/id-481480/";
}
