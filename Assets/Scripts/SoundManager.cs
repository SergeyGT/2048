using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;
    
    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private AudioClip victoryMusic;
    [SerializeField] private AudioClip gameOverMusic;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;
    
    [Header("Game SFX")]
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip mergeSound;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip enemyDeathSound;
    [SerializeField] private AudioClip tileSpawnSound;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip gameOverSound;
    
    [Header("UI SFX")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip scoreIncreaseSound;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float uiVolume = 0.6f;
    
    [Header("Pitch Variation")]
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float pitchRange = 0.1f;
    
    private float originalSfxPitch = 1f;
    private bool isMuted = false;
    
    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeAudioSources();
    }
    
    private void InitializeAudioSources()
    {
        // Создаём AudioSource для музыки если не назначен
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }
        
        // Создаём AudioSource для звуковых эффектов
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
            originalSfxPitch = sfxSource.pitch;
        }
        
        // Создаём AudioSource для UI звуков
        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.loop = false;
            uiSource.playOnAwake = false;
            uiSource.volume = uiVolume;
        }
    }
    
    private void Start()
    {
        PlayBackgroundMusic();
    }
    
    // ========== МУЗЫКА ==========
    
    public void PlayBackgroundMusic()
    {
        if (backgroundMusic == null || musicSource == null) return;
        
        musicSource.clip = backgroundMusic;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }
    
    public void PlayBossMusic()
    {
        if (bossMusic == null) return;
        
        musicSource.clip = bossMusic;
        musicSource.Play();
    }
    
    public void PlayVictoryMusic()
    {
        if (victoryMusic == null) return;
        
        musicSource.clip = victoryMusic;
        musicSource.loop = false;
        musicSource.Play();
    }
    
    public void PlayGameOverMusic()
    {
        if (gameOverMusic == null) return;
        
        musicSource.clip = gameOverMusic;
        musicSource.loop = false;
        musicSource.Play();
    }
    
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }
    
    public void PauseMusic()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
        }
    }
    
    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }
    
    // ========== ИГРОВЫЕ ЗВУКИ ==========
    
    public void PlayMoveSound()
    {
        PlaySFX(moveSound);
    }
    
    public void PlayMergeSound(int mergeLevel = 1)
    {
        if (mergeSound == null) return;
        
        // Повышаем pitch для высокоуровневых слияний
        float pitchMultiplier = 1f + (mergeLevel * 0.05f);
        pitchMultiplier = Mathf.Min(pitchMultiplier, 1.5f);
        
        PlaySFX(mergeSound, pitchMultiplier);
    }
    
    public void PlayDamageSound()
    {
        PlaySFX(damageSound);
    }
    
    public void PlayEnemyDeathSound()
    {
        PlaySFX(enemyDeathSound);
    }
    
    public void PlayTileSpawnSound()
    {
        PlaySFX(tileSpawnSound, 0.8f);
    }
    
    public void PlayVictorySound()
    {
        PlaySFX(victorySound);
    }
    
    public void PlayGameOverSound()
    {
        PlaySFX(gameOverSound);
    }
    
    // ========== UI ЗВУКИ ==========
    
    public void PlayButtonClick()
    {
        PlayUISFX(buttonClickSound);
    }
    
    public void PlayScoreIncrease()
    {
        PlayUISFX(scoreIncreaseSound);
    }
    
    // ========== ОСНОВНЫЕ МЕТОДЫ ==========
    
    private void PlaySFX(AudioClip clip, float pitchMultiplier = 1f)
    {
        if (clip == null || sfxSource == null || isMuted) return;
        
        float originalPitch = sfxSource.pitch;
        
        if (randomizePitch)
        {
            float randomPitch = Random.Range(-pitchRange, pitchRange);
            sfxSource.pitch = (originalSfxPitch + randomPitch) * pitchMultiplier;
        }
        else
        {
            sfxSource.pitch = originalSfxPitch * pitchMultiplier;
        }
        
        sfxSource.PlayOneShot(clip, sfxVolume);
        
        // Восстанавливаем оригинальный pitch
        sfxSource.pitch = originalSfxPitch;
    }
    
    private void PlayUISFX(AudioClip clip)
    {
        if (clip == null || uiSource == null || isMuted) return;
        
        uiSource.PlayOneShot(clip, uiVolume);
    }
    
    // ========== НАСТРОЙКИ ==========
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
        
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }
    
    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("UIVolume", uiVolume);
    }
    
    public void MuteAll()
    {
        isMuted = true;
        musicSource.mute = true;
        sfxSource.mute = true;
        uiSource.mute = true;
    }
    
    public void UnmuteAll()
    {
        isMuted = false;
        musicSource.mute = false;
        sfxSource.mute = false;
        uiSource.mute = false;
    }
    
    public void ToggleMute()
    {
        if (isMuted)
        {
            UnmuteAll();
        }
        else
        {
            MuteAll();
        }
    }
    
    public void LoadVolumeSettings()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
        uiVolume = PlayerPrefs.GetFloat("UIVolume", 0.6f);
        
        if (musicSource != null) musicSource.volume = musicVolume;
    }
    
    // ========== ЭФФЕКТЫ ==========
    
    public void FadeOutMusic(float duration = 1f)
    {
        StartCoroutine(FadeMusicCoroutine(musicVolume, 0f, duration));
    }
    
    public void FadeInMusic(float duration = 1f)
    {
        StartCoroutine(FadeMusicCoroutine(0f, musicVolume, duration));
    }
    
    private IEnumerator FadeMusicCoroutine(float from, float to, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            musicSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }
        
        musicSource.volume = to;
    }
    
    public void CrossfadeMusic(AudioClip newClip, float duration = 1f)
    {
        StartCoroutine(CrossfadeCoroutine(newClip, duration));
    }
    
    private IEnumerator CrossfadeCoroutine(AudioClip newClip, float duration)
    {
        // Fade out
        yield return FadeMusicCoroutine(musicVolume, 0f, duration / 2f);
        
        // Change clip
        musicSource.clip = newClip;
        musicSource.Play();
        
        // Fade in
        yield return FadeMusicCoroutine(0f, musicVolume, duration / 2f);
    }
}