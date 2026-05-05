using UnityEngine;

namespace PixelShooter3D
{
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Available Clips")]
    public AudioClip jumpClip;
    public AudioClip loseClip;
    public AudioClip popClip;
    public AudioClip shootClip;
    public AudioClip winClip;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep sound across levels
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    // --- Mappings ---

    // UI Interactions
    public void PlayClick() => PlaySFX(popClip);
    public void PlayPopupOpen() => PlaySFX(popClip);

    // Game Flow
    public void PlayWin() => PlaySFX(winClip);
    public void PlayLose() => PlaySFX(loseClip);

    // Gameplay Actions
    public void PlayJump() => PlaySFX(jumpClip); // Used for Tray/Pig movement
    public void PlayShoot() => PlaySFX(shootClip, 0.6f); // Lower volume slightly
    public void PlayPigSelect() => PlaySFX(popClip); // "Pop" fits selecting a pig
    public void PlayPowerup() => PlaySFX(jumpClip); // "Jump" sound for powerup activation feels energetic
}
}