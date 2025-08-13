using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Система звуков животного. Управляет всеми аудиоэффектами и их воспроизведением.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AnimalAudio : MonoBehaviour
{
    [Header("Voice Sounds")]
    [SerializeField] private AudioClip[] idleSounds; // Случайные звуки в покое
    [SerializeField] private AudioClip[] hurtSounds; // Звуки боли
    [SerializeField] private AudioClip[] deathSounds; // Звуки смерти
    [SerializeField] private AudioClip[] eatingSounds; // Звуки поедания
    [SerializeField] private AudioClip[] alertSounds; // Звуки тревоги
    
    [Header("Movement Sounds")]
    [SerializeField] private AudioClip[] footstepSounds; // Звуки шагов
    [SerializeField] private AudioClip[] runFootstepSounds; // Звуки бега
    [SerializeField] private AudioClip[] grassWalkSounds; // Звуки хождения по траве
    
    [Header("Interaction Sounds")]
    [SerializeField] private AudioClip[] grassEatingSounds; // Звуки жевания травы
    [SerializeField] private AudioClip[] drinkingSounds; // Звуки питья
    [SerializeField] private AudioClip[] sniffingSounds; // Звуки нюханья
    
    [Header("Audio Settings")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float voiceVolume = 0.8f;
    [SerializeField] private float footstepVolume = 0.4f;
    [SerializeField] private float interactionVolume = 0.6f;
    [SerializeField] private bool use3DAudio = true;
    [SerializeField] private float maxHearingDistance = 20f;
    
    [Header("Randomization")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);
    [SerializeField] private Vector2 volumeVariation = new Vector2(0.8f, 1f);
    [SerializeField] private bool randomizeDelay = true;
    [SerializeField] private Vector2 randomDelayRange = new Vector2(0f, 0.2f);
    
    [Header("Idle Sound Settings")]
    [SerializeField] private float idleSoundInterval = 15f; // Интервал случайных звуков
    [SerializeField] private Vector2 idleIntervalRange = new Vector2(10f, 30f);
    [SerializeField] private float idleSoundChance = 0.3f; // Шанс издать звук
    
    [Header("Footstep Settings")]
    [SerializeField] private float footstepInterval = 0.5f; // Интервал между шагами
    [SerializeField] private float minSpeedForFootsteps = 0.1f;
    [SerializeField] private bool footstepsBasedOnSpeed = true;
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioSource interactionAudioSource;
    
    // Компоненты
    private AnimalMovement movement;
    private AnimalHealth health;
    private AnimalAnimator animalAnimator;
    private AnimalStats stats;
    
    // Состояние воспроизведения
    private bool isPlayingVoiceSound = false;
    private bool isPlayingInteractionSound = false;
    private float lastFootstepTime = 0f;
    private float lastIdleSoundTime = 0f;
    private float nextIdleSoundTime = 0f;
    
    // Корутины
    private Coroutine idleSoundCoroutine;
    private Coroutine footstepCoroutine;
    
    // Кэш для производительности
    private readonly Dictionary<AudioClip, float> clipLengthCache = new Dictionary<AudioClip, float>();
    
    // События
    public System.Action<AudioClip> OnSoundPlayed;
    public System.Action<string> OnSoundCategoryPlayed; // voice, footstep, interaction
    
    // Публичные свойства
    public bool IsPlayingVoiceSound => isPlayingVoiceSound;
    public bool IsPlayingInteractionSound => isPlayingInteractionSound;
    public float MasterVolume => masterVolume;
    
    private void Awake()
    {
        // Получаем компоненты
        movement = GetComponent<AnimalMovement>();
        health = GetComponent<AnimalHealth>();
        animalAnimator = GetComponent<AnimalAnimator>();
        
        // Настраиваем AudioSources
        SetupAudioSources();
    }
    
    private void Start()
    {
        // Подписываемся на события
        SubscribeToEvents();
        
        // Запускаем системы звуков
        StartIdleSounds();
        StartFootstepSystem();
        
        // Планируем первый idle звук
        ScheduleNextIdleSound();
    }
    
    /// <summary>
    /// Инициализация аудиосистемы
    /// </summary>
    public void Initialize(AnimalStats animalStats)
    {
        stats = animalStats;
        
        if (stats != null)
        {
            // Применяем настройки из ScriptableObject
            masterVolume = stats.audioVolume;
            maxHearingDistance = stats.maxHearingDistance;
        }
        
        ApplyAudioSettings();
    }
    
    #region Audio Source Setup
    
    private void SetupAudioSources()
    {
        // Основной AudioSource для голоса
        if (voiceAudioSource == null)
        {
            voiceAudioSource = GetComponent<AudioSource>();
        }
        
        // AudioSource для шагов
        if (footstepAudioSource == null)
        {
            GameObject footstepObj = new GameObject("FootstepAudioSource");
            footstepObj.transform.SetParent(transform);
            footstepObj.transform.localPosition = Vector3.zero;
            footstepAudioSource = footstepObj.AddComponent<AudioSource>();
        }
        
        // AudioSource для взаимодействий
        if (interactionAudioSource == null)
        {
            GameObject interactionObj = new GameObject("InteractionAudioSource");
            interactionObj.transform.SetParent(transform);
            interactionObj.transform.localPosition = Vector3.zero;
            interactionAudioSource = interactionObj.AddComponent<AudioSource>();
        }
        
        ApplyAudioSettings();
    }
    
    private void ApplyAudioSettings()
    {
        AudioSource[] allSources = { voiceAudioSource, footstepAudioSource, interactionAudioSource };
        
        foreach (var source in allSources)
        {
            if (source == null) continue;
            
            source.spatialBlend = use3DAudio ? 1f : 0f; // 3D или 2D звук
            source.maxDistance = maxHearingDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f; // Отключаем эффект Доплера
            source.playOnAwake = false;
        }
        
        // Индивидуальные настройки громкости
        if (voiceAudioSource != null)
            voiceAudioSource.volume = masterVolume * voiceVolume;
        
        if (footstepAudioSource != null)
            footstepAudioSource.volume = masterVolume * footstepVolume;
        
        if (interactionAudioSource != null)
            interactionAudioSource.volume = masterVolume * interactionVolume;
    }
    
    #endregion
    
    #region Voice Sounds
    
    /// <summary>
    /// Воспроизвести случайный звук в покое
    /// </summary>
    public void PlayIdleSound()
    {
        PlayRandomSound(idleSounds, voiceAudioSource, "idle");
    }
    
    /// <summary>
    /// Воспроизвести звук боли
    /// </summary>
    public void PlayHurtSound()
    {
        PlayRandomSound(hurtSounds, voiceAudioSource, "hurt");
    }
    
    /// <summary>
    /// Воспроизвести звук смерти
    /// </summary>
    public void PlayDeathSound()
    {
        PlayRandomSound(deathSounds, voiceAudioSource, "death");
    }
    
    /// <summary>
    /// Воспроизвести звук поедания
    /// </summary>
    public void PlayEatingSound()
    {
        PlayRandomSound(eatingSounds, voiceAudioSource, "eating");
    }
    
    /// <summary>
    /// Воспроизвести звук тревоги
    /// </summary>
    public void PlayAlertSound()
    {
        PlayRandomSound(alertSounds, voiceAudioSource, "alert");
    }
    
    #endregion
    
    #region Movement Sounds
    
    /// <summary>
    /// Воспроизвести звук шага
    /// </summary>
    public void PlayFootstepSound()
    {
        if (movement == null) return;
        
        AudioClip[] stepsToUse = movement.CurrentSpeed > (stats?.runSpeed ?? 5f) ? 
            runFootstepSounds : footstepSounds;
        
        // Используем звуки травы если идем по траве
        if (IsOnGrass())
        {
            stepsToUse = grassWalkSounds;
        }
        
        PlayRandomSound(stepsToUse, footstepAudioSource, "footstep");
    }
    
    private void HandleAutomaticFootsteps()
    {
        if (movement == null || !footstepsBasedOnSpeed) return;
        
        float currentSpeed = movement.CurrentSpeed;
        
        if (currentSpeed >= minSpeedForFootsteps && movement.IsGrounded)
        {
            // Частота шагов зависит от скорости
            float speedBasedInterval = footstepInterval / Mathf.Max(1f, currentSpeed / 2f);
            
            if (Time.time - lastFootstepTime >= speedBasedInterval)
            {
                PlayFootstepSound();
                lastFootstepTime = Time.time;
            }
        }
    }
    
    #endregion
    
    #region Interaction Sounds
    
    /// <summary>
    /// Воспроизвести звук жевания травы
    /// </summary>
    public void PlayGrassEatingSound()
    {
        PlayRandomSound(grassEatingSounds, interactionAudioSource, "grass_eating");
    }
    
    /// <summary>
    /// Воспроизвести звук питья
    /// </summary>
    public void PlayDrinkingSound()
    {
        PlayRandomSound(drinkingSounds, interactionAudioSource, "drinking");
    }
    
    /// <summary>
    /// Воспроизвести звук нюханья
    /// </summary>
    public void PlaySniffingSound()
    {
        PlayRandomSound(sniffingSounds, interactionAudioSource, "sniffing");
    }
    
    #endregion
    
    #region Sound Playing Logic
    
    private void PlayRandomSound(AudioClip[] clips, AudioSource audioSource, string category)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;
        
        // Выбираем случайный клип
        AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];
        if (clipToPlay == null) return;
        
        StartCoroutine(PlaySoundCoroutine(clipToPlay, audioSource, category));
    }
    
    private IEnumerator PlaySoundCoroutine(AudioClip clip, AudioSource audioSource, string category)
    {
        // Случайная задержка
        if (randomizeDelay)
        {
            float delay = Random.Range(randomDelayRange.x, randomDelayRange.y);
            if (delay > 0) yield return new WaitForSeconds(delay);
        }
        
        // Останавливаем текущий звук если нужно
        if (audioSource.isPlaying && category == "voice")
        {
            audioSource.Stop();
        }
        
        // Настраиваем параметры
        audioSource.clip = clip;
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        
        // Случайная вариация громкости
        float originalVolume = audioSource.volume;
        audioSource.volume = originalVolume * Random.Range(volumeVariation.x, volumeVariation.y);
        
        // Воспроизводим
        audioSource.Play();
        
        // Отмечаем состояние
        if (category == "voice") isPlayingVoiceSound = true;
        if (category.Contains("eating") || category.Contains("drinking")) isPlayingInteractionSound = true;
        
        // События
        OnSoundPlayed?.Invoke(clip);
        OnSoundCategoryPlayed?.Invoke(category);
        
        // Ждем окончания
        float clipLength = GetClipLength(clip);
        yield return new WaitForSeconds(clipLength);
        
        // Восстанавливаем состояние
        audioSource.volume = originalVolume;
        
        if (category == "voice") isPlayingVoiceSound = false;
        if (category.Contains("eating") || category.Contains("drinking")) isPlayingInteractionSound = false;
    }
    
    private float GetClipLength(AudioClip clip)
    {
        if (clip == null) return 0f;
        
        if (clipLengthCache.ContainsKey(clip))
        {
            return clipLengthCache[clip];
        }
        
        float length = clip.length;
        clipLengthCache[clip] = length;
        return length;
    }
    
    #endregion
    
    #region Idle Sound System
    
    private void StartIdleSounds()
    {
        if (idleSoundCoroutine == null)
        {
            idleSoundCoroutine = StartCoroutine(IdleSoundLoop());
        }
    }
    
    private IEnumerator IdleSoundLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Проверяем каждую секунду
            
            if (Time.time >= nextIdleSoundTime && !isPlayingVoiceSound)
            {
                // Проверяем условия для idle звука
                if (CanPlayIdleSound())
                {
                    if (Random.value <= idleSoundChance)
                    {
                        PlayIdleSound();
                    }
                }
                
                ScheduleNextIdleSound();
            }
        }
    }
    
    private bool CanPlayIdleSound()
    {
        if (health != null && health.IsDead) return false;
        if (movement != null && movement.IsMoving) return false;
        if (animalAnimator != null && animalAnimator.IsEating) return false;
        
        return true;
    }
    
    private void ScheduleNextIdleSound()
    {
        float interval = Random.Range(idleIntervalRange.x, idleIntervalRange.y);
        nextIdleSoundTime = Time.time + interval;
    }
    
    #endregion
    
    #region Footstep System
    
    private void StartFootstepSystem()
    {
        if (footstepCoroutine == null && footstepsBasedOnSpeed)
        {
            footstepCoroutine = StartCoroutine(FootstepLoop());
        }
    }
    
    private IEnumerator FootstepLoop()
    {
        while (true)
        {
            HandleAutomaticFootsteps();
            yield return new WaitForSeconds(0.1f); // Проверяем 10 раз в секунду
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    private void SubscribeToEvents()
    {
        if (health != null)
        {
            health.OnDamageTaken += HandleDamage;
            health.OnDeath += HandleDeath;
        }
        
        if (animalAnimator != null)
        {
            animalAnimator.OnAnimationEvent += HandleAnimationEvent;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (health != null)
        {
            health.OnDamageTaken -= HandleDamage;
            health.OnDeath -= HandleDeath;
        }
        
        if (animalAnimator != null)
        {
            animalAnimator.OnAnimationEvent -= HandleAnimationEvent;
        }
    }
    
    private void HandleDamage(float damage)
    {
        PlayHurtSound();
    }
    
    private void HandleDeath()
    {
        PlayDeathSound();
        
        // Останавливаем idle звуки
        if (idleSoundCoroutine != null)
        {
            StopCoroutine(idleSoundCoroutine);
            idleSoundCoroutine = null;
        }
    }
    
    private void HandleAnimationEvent(string eventName)
    {
        switch (eventName)
        {
            case "Footstep":
                PlayFootstepSound();
                break;
            case "EatBite":
                PlayGrassEatingSound();
                break;
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Установить общую громкость
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
    }
    
    /// <summary>
    /// Проверить, стоит ли животное на траве
    /// </summary>
    private bool IsOnGrass()
    {
        // Простая проверка через raycast
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 1f))
        {
            return hit.collider.CompareTag("Grass") || hit.collider.name.ToLower().Contains("grass");
        }
        return false;
    }
    
    /// <summary>
    /// Остановить все звуки
    /// </summary>
    public void StopAllSounds()
    {
        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
            voiceAudioSource.Stop();
        
        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            footstepAudioSource.Stop();
        
        if (interactionAudioSource != null && interactionAudioSource.isPlaying)
            interactionAudioSource.Stop();
        
        isPlayingVoiceSound = false;
        isPlayingInteractionSound = false;
    }
    
    /// <summary>
    /// Приглушить все звуки (для кат-сцен)
    /// </summary>
    public void MuteTemporarily(bool mute)
    {
        float targetVolume = mute ? 0f : masterVolume;
        
        if (voiceAudioSource != null)
            voiceAudioSource.volume = targetVolume * voiceVolume;
        
        if (footstepAudioSource != null)
            footstepAudioSource.volume = targetVolume * footstepVolume;
        
        if (interactionAudioSource != null)
            interactionAudioSource.volume = targetVolume * interactionVolume;
    }
    
    /// <summary>
    /// Добавить новые звуки во время выполнения
    /// </summary>
    public void AddIdleSounds(AudioClip[] newClips)
    {
        if (newClips == null || newClips.Length == 0) return;
        
        List<AudioClip> currentClips = new List<AudioClip>(idleSounds);
        currentClips.AddRange(newClips);
        idleSounds = currentClips.ToArray();
    }
    
    #endregion
    
    #region Debug
    
    /// <summary>
    /// Проиграть тестовый звук
    /// </summary>
    [ContextMenu("Test Idle Sound")]
    public void TestIdleSound()
    {
        PlayIdleSound();
    }
    
    [ContextMenu("Test Hurt Sound")]
    public void TestHurtSound()
    {
        PlayHurtSound();
    }
    
    [ContextMenu("Test Footstep Sound")]
    public void TestFootstepSound()
    {
        PlayFootstepSound();
    }
    
    /// <summary>
    /// Логирование информации об аудиосистеме
    /// </summary>
    [ContextMenu("Log Audio Info")]
    public void LogAudioInfo()
    {
        Debug.Log($"=== Audio Info for {gameObject.name} ===");
        Debug.Log($"Master Volume: {masterVolume}");
        Debug.Log($"Voice Playing: {isPlayingVoiceSound}");
        Debug.Log($"Interaction Playing: {isPlayingInteractionSound}");
        Debug.Log($"Idle Sounds: {idleSounds?.Length ?? 0}");
        Debug.Log($"Hurt Sounds: {hurtSounds?.Length ?? 0}");
        Debug.Log($"Footstep Sounds: {footstepSounds?.Length ?? 0}");
        Debug.Log($"Next Idle Sound: {nextIdleSoundTime - Time.time:F1}s");
    }
    
    #endregion
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        if (idleSoundCoroutine != null)
        {
            StopCoroutine(idleSoundCoroutine);
        }
        
        if (footstepCoroutine != null)
        {
            StopCoroutine(footstepCoroutine);
        }
    }
}