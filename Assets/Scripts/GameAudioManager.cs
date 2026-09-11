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

    [Header("Background Music - assign later")]
    public AudioClip backgroundMusic;

    [Header("Sound Effects - assign later")]
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
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
        ApplyVolumes();
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
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    private void ApplyVolumes()
    {
        EnsureSources();
        float mute = Muted ? 0f : 1f;
        musicSource.volume = BgmVolume * mute;
        sfxSource.volume = SfxVolume * mute;
        if (backgroundMusic == null) return;
        if (musicSource.clip != backgroundMusic) musicSource.clip = backgroundMusic;
        if (!musicSource.isPlaying) musicSource.Play();
    }
}
