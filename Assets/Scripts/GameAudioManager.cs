using UnityEngine;

public enum GameSfx
{
    Button,
    Move,
    Pearl,
    Obstacle,
    PowerUp,
    GameOver
}

/// <summary>Assign clips on the Game Audio object in the Gameplay scene.</summary>
public class GameAudioManager : MonoBehaviour
{
    private const string BgmVolumeKey = "Fishfish.Audio.BGM";
    private const string SfxVolumeKey = "Fishfish.Audio.SFX";
    private const string MutedKey = "Fishfish.Audio.Muted";

    public static GameAudioManager Instance { get; private set; }

    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("Sound Effects")]
    public AudioClip buttonClick;
    public AudioClip playerMove;
    public AudioClip pearlCollected;
    public AudioClip obstacleHit;
    public AudioClip powerUpCollected;
    public AudioClip gameOver;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    public static float BgmVolume => PlayerPrefs.GetFloat(BgmVolumeKey, 0.7f);
    public static float SfxVolume => PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
    public static bool Muted => PlayerPrefs.GetInt(MutedKey, 0) == 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A manager from an earlier scene can exist without any clips.
            // Transfer this scene's assignments before discarding its duplicate.
            Instance.CopyAssignedClipsFrom(this);
            Instance.ApplyVolumes();
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
        ApplyVolumes();
    }

    private void CopyAssignedClipsFrom(GameAudioManager other)
    {
        if (other.backgroundMusic != null) backgroundMusic = other.backgroundMusic;
        if (other.buttonClick != null) buttonClick = other.buttonClick;
        if (other.playerMove != null) playerMove = other.playerMove;
        if (other.pearlCollected != null) pearlCollected = other.pearlCollected;
        if (other.obstacleHit != null) obstacleHit = other.obstacleHit;
        if (other.powerUpCollected != null) powerUpCollected = other.powerUpCollected;
        if (other.gameOver != null) gameOver = other.gameOver;
    }

    public static GameAudioManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        GameAudioManager existing = FindAnyObjectByType<GameAudioManager>();
        if (existing != null) return existing;
        return new GameObject("Game Audio").AddComponent<GameAudioManager>();
    }

    public static void SetBgmVolume(float value)
    {
        PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        EnsureInstance().ApplyVolumes();
    }

    public static void SetSfxVolume(float value)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        EnsureInstance().ApplyVolumes();
    }

    public static void ToggleMute()
    {
        PlayerPrefs.SetInt(MutedKey, Muted ? 0 : 1);
        PlayerPrefs.Save();
        EnsureInstance().ApplyVolumes();
    }

    public static void Play(GameSfx sound)
    {
        GameAudioManager manager = EnsureInstance();
        AudioClip clip = sound switch
        {
            GameSfx.Button => manager.buttonClick,
            GameSfx.Move => manager.playerMove,
            GameSfx.Pearl => manager.pearlCollected,
            GameSfx.Obstacle => manager.obstacleHit,
            GameSfx.PowerUp => manager.powerUpCollected,
            GameSfx.GameOver => manager.gameOver,
            _ => null
        };
        if (clip != null && !Muted) manager.sfxSource.PlayOneShot(clip);
    }

    private void EnsureSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    private void ApplyVolumes()
    {
        EnsureSources();
        LoadMissingClips();
        float mute = Muted ? 0f : 1f;
        musicSource.volume = BgmVolume * mute;
        sfxSource.volume = SfxVolume * mute;
        if (backgroundMusic == null) return;
        if (musicSource.clip != backgroundMusic) musicSource.clip = backgroundMusic;
        if (!musicSource.isPlaying) musicSource.Play();
    }

    private void LoadMissingClips()
    {
        // Also works when an already-open scene has not refreshed its serialized
        // clip references, or an audio manager was created before Gameplay loaded.
        if (backgroundMusic == null) backgroundMusic = Resources.Load<AudioClip>("GameAudio/BGM");
        if (buttonClick == null) buttonClick = Resources.Load<AudioClip>("GameAudio/ButtonClick");
        if (playerMove == null) playerMove = Resources.Load<AudioClip>("GameAudio/PlayerMove");
        if (pearlCollected == null) pearlCollected = Resources.Load<AudioClip>("GameAudio/PearlCollected");
        if (obstacleHit == null) obstacleHit = Resources.Load<AudioClip>("GameAudio/ObstacleHit");
        if (powerUpCollected == null) powerUpCollected = Resources.Load<AudioClip>("GameAudio/PowerUpCollected");
        if (gameOver == null) gameOver = Resources.Load<AudioClip>("GameAudio/GameOver");
    }
}
