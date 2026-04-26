using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }
    
    [Header("Enemy Settings")]
    [SerializeField] private EnemyState[] enemyStates;
    [SerializeField] private GameObject enemyUIPrefab;
    [SerializeField] private Transform enemyUIContainer;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private Image enemyPortraitImage;
    [SerializeField] private Transform pointMobile;
    
    [Header("Progress Bar")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI progressPercentageText;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Color progressNormalColor = Color.green;
    [SerializeField] private Color progressBossColor = Color.red;
    
    private Enemy currentEnemy;
    private GameObject currentEnemyUI;
    private int currentEnemyIndex = 0;
    private int enemiesKilled = 0;
    private int savedEnemyHealth = -1;
    private bool allDefeated = false;
    private Coroutine respawnCoroutine;
    
    private const string ENEMY_INDEX_KEY = "EnemyIndex";
    private const string ENEMY_HEALTH_KEY = "EnemyHealth";
    private const string ENEMIES_KILLED_KEY = "EnemiesKilled";
    private const string ALL_DEFEATED_KEY = "AllDefeated";
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
    }
    
    private void Start()
    {
        
    LoadProgress();
    
    // Проверяем флаг полной победы
    if (allDefeated)
    {
        Debug.Log("All enemies already defeated!");
        ShowVictoryState();
        return;
    }
    
    // Проверяем индекс
    if (currentEnemyIndex >= enemyStates.Length)
    {
        SetAllDefeated();
        ShowVictoryState();
        return;
    }
    
    // ★ Запускаем первый спавн с небольшой задержкой
    StartCoroutine(InitialSpawn());
    }

private IEnumerator InitialSpawn()
{
    yield return null; // ★ Ждём один кадр, чтобы все компоненты инициализировались
    SpawnCurrentEnemy();
    UpdateProgressBar();
}
    
    private void OnDestroy()
    {
        // Останавливаем корутину при уничтожении объекта
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void OnApplicationQuit()
    {
        SaveProgress();
    }
    
    private void ShowVictoryState()
    {
        // Останавливаем респавн если есть
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }
        
        // Уничтожаем любого оставшегося врага
        if (currentEnemyUI != null)
        {
            Destroy(currentEnemyUI);
            currentEnemyUI = null;
            currentEnemy = null;
        }
        
        if (enemyNameText != null)
            enemyNameText.text = "VICTORY!";
        if (enemyPortraitImage != null)
            enemyPortraitImage.gameObject.SetActive(false);
        
        UpdateProgressBar();
    }
    
    private void SetAllDefeated()
    {
        allDefeated = true;
        currentEnemyIndex = enemyStates.Length;
        enemiesKilled = enemyStates.Length;
        savedEnemyHealth = -1;
        SaveProgress();
    }
    
    private void SaveProgress()
    {
        PlayerPrefs.SetInt(ENEMY_INDEX_KEY, currentEnemyIndex);
        PlayerPrefs.SetInt(ENEMIES_KILLED_KEY, enemiesKilled);
        PlayerPrefs.SetInt(ALL_DEFEATED_KEY, allDefeated ? 1 : 0);
        
        if (currentEnemy != null && !currentEnemy.isDead)
        {
            PlayerPrefs.SetInt(ENEMY_HEALTH_KEY, currentEnemy.currentHealth);
        }
        else
        {
            PlayerPrefs.SetInt(ENEMY_HEALTH_KEY, -1);
        }
        
        PlayerPrefs.Save();
        Debug.Log($"💾 Saved: Index={currentEnemyIndex}, Killed={enemiesKilled}, AllDefeated={allDefeated}");
    }
    
    private void LoadProgress()
    {
    currentEnemyIndex = PlayerPrefs.GetInt(ENEMY_INDEX_KEY, 0);
    enemiesKilled = PlayerPrefs.GetInt(ENEMIES_KILLED_KEY, 0);
    savedEnemyHealth = PlayerPrefs.GetInt(ENEMY_HEALTH_KEY, -1);
    allDefeated = PlayerPrefs.GetInt(ALL_DEFEATED_KEY, 0) == 1;
    
    if (currentEnemyIndex >= enemyStates.Length && !allDefeated)
    {
        allDefeated = true;
        enemiesKilled = enemyStates.Length;
        SaveProgress();
    }
    
    Debug.Log($"📂 Loaded: Index={currentEnemyIndex}, Killed={enemiesKilled}, AllDefeated={allDefeated}");
    
    // ✅ Инициализируем фон с правильным индексом после загрузки
    BackgroundManager.Instance?.InitializeBackground(currentEnemyIndex, allDefeated);
    }
    
    private void UpdateProgressBar()
    {
    if (progressBar == null) return;
    
    int totalEnemies = enemyStates.Length;
    
    progressBar.maxValue = totalEnemies;
    
    // Всегда используем enemiesKilled для прогресс-бара
    progressBar.value = Mathf.Clamp(enemiesKilled, 0, totalEnemies);
    
    if (progressText != null)
    {
        if (allDefeated)
        {
            progressText.text = "ALL CLEAR!";
        }
        else if (currentEnemyIndex < totalEnemies)
        {
            progressText.text = $"{enemyStates[currentEnemyIndex].enemyName}";
        }
    }
    
    if (progressPercentageText != null)
    {
        progressPercentageText.text = $"{enemiesKilled}/{totalEnemies}";
    }
    
    if (progressFillImage != null)
    {
        progressFillImage.color = allDefeated ? progressBossColor : progressNormalColor;
    }
    }
    
    [ContextMenu("Reset Enemy Progress")]
    public void ResetProgress()
    {
        // Останавливаем респавн
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }
        
        allDefeated = false;
        currentEnemyIndex = 0;
        enemiesKilled = 0;
        savedEnemyHealth = -1;
        
        PlayerPrefs.DeleteKey(ENEMY_INDEX_KEY);
        PlayerPrefs.DeleteKey(ENEMY_HEALTH_KEY);
        PlayerPrefs.DeleteKey(ENEMIES_KILLED_KEY);
        PlayerPrefs.DeleteKey(ALL_DEFEATED_KEY);
        PlayerPrefs.Save();
        
        // Уничтожаем старого врага если есть
        if (currentEnemyUI != null)
        {
            Destroy(currentEnemyUI);
            currentEnemyUI = null;
            currentEnemy = null;
        }
        
        // Включаем отображение
        if (enemyPortraitImage != null)
            enemyPortraitImage.gameObject.SetActive(true);
        if (enemyNameText != null)
            enemyNameText.text = "";
            
        SpawnCurrentEnemy();
        UpdateProgressBar();
        
        Debug.Log("🔄 Enemy progress RESET!");
    }
    
    private void SpawnCurrentEnemy()
    {
        // Усиленная проверка
        if (allDefeated)
        {
            Debug.Log("❌ Cannot spawn - all enemies defeated (allDefeated flag)!");
            ShowVictoryState();
            return;
        }
        
        if (currentEnemyIndex >= enemyStates.Length)
        {
            Debug.Log("❌ Cannot spawn - index out of range! Setting all defeated.");
            SetAllDefeated();
            ShowVictoryState();
            return;
        }
        
        // Проверяем, что враг ещё не существует
        if (currentEnemy != null || currentEnemyUI != null)
        {
            Debug.LogWarning("⚠️ Enemy already exists! Destroying before spawn...");
            if (currentEnemyUI != null)
            {
                Destroy(currentEnemyUI);
                currentEnemyUI = null;
                currentEnemy = null;
            }
        }
        
        EnemyState selectedState = enemyStates[currentEnemyIndex];
        Debug.Log($"🐣 Spawning enemy: {selectedState.enemyName} at index {currentEnemyIndex}");
        
        SpawnEnemyUI(selectedState);
        UpdateEnemyDisplay(selectedState);
        
        if (currentEnemy != null && savedEnemyHealth > 0)
        {
            currentEnemy.SetHealth(savedEnemyHealth);
            Debug.Log($"❤️ Restored health: {savedEnemyHealth}/{selectedState.maxHealth}");
            savedEnemyHealth = -1;
        }
        
        UpdateProgressBar();
    }
    
    private void SpawnEnemyUI(EnemyState state)
    {
    // Очищаем старые ссылки и уничтожаем старого врага
    if (currentEnemyUI != null)
    {
        // Отключаем врага перед уничтожением чтобы не вызвать OnDeath
        if (currentEnemy != null)
        {
            currentEnemy.enabled = false; // ★ Отключаем скрипт
        }
        
        GameObject oldUI = currentEnemyUI;
        currentEnemyUI = null;
        currentEnemy = null;
        
        // Принудительно уничтожаем немедленно, а не в конце кадра
        if (Application.isPlaying)
            Destroy(oldUI);
        else
            DestroyImmediate(oldUI);
    }
    
    // Дополнительно: ищем и уничтожаем все объекты врагов в контейнере
    if (enemyUIContainer != null)
    {
        foreach (Transform child in enemyUIContainer)
        {
            if (child.gameObject.name.StartsWith("Enemy_"))
            {
                Debug.LogWarning($"🧹 Cleaning up leftover enemy: {child.name}");
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
    
    if (enemyUIPrefab != null && enemyUIContainer != null)
    {
        currentEnemyUI = Instantiate(enemyUIPrefab, enemyUIContainer);
        currentEnemyUI.name = $"Enemy_{state.enemyName}";
        
        RectTransform rect = currentEnemyUI.GetComponent<RectTransform>();
        if (rect != null)
        {
            if (Application.isMobilePlatform) 
                rect.anchoredPosition = pointMobile.localPosition;
        }
        
        currentEnemy = currentEnemyUI.GetComponent<Enemy>();
        
        if (currentEnemy != null)
        {
            currentEnemy.enabled = true; // ★ Включаем скрипт обратно
            currentEnemy.Initialize(state);
        }
    }
    }
    
    private void UpdateEnemyDisplay(EnemyState state)
    {
        if (enemyNameText != null)
            enemyNameText.text = state.enemyName;
            
        if (enemyPortraitImage != null)
        {
            enemyPortraitImage.sprite = state.icon;
            enemyPortraitImage.gameObject.SetActive(true);
        }
        
        UpdateProgressBar();
    }
    
    public void ProcessMergeDamage(int mergePower, Vector2Int position)
    {
        // Усиленная проверка
        if (currentEnemy == null || currentEnemyUI == null) 
        {
            Debug.Log("⚠️ Cannot damage - no enemy object");
            return;
        }
        
        if (currentEnemy.isDead || allDefeated) 
        {
            Debug.Log("⚠️ Cannot damage - enemy dead or all defeated");
            return;
        }
        
        int damage = currentEnemy.GetDamageForMerge(mergePower);
        currentEnemy.TakeDamage(damage);
        
        SoundManager.Instance?.PlayDamageSound();
    }
    
    public void OnEnemyDeath(Enemy enemy)
    {
    Debug.Log($"☠️ OnEnemyDeath called. Enemy: {(enemy != null ? enemy.name : "null")}, CurrentEnemy: {(currentEnemy != null ? currentEnemy.name : "null")}");
    
    // Проверка: не обрабатываем смерть если все уже побеждены
    if (allDefeated)
    {
        Debug.Log("⚠️ OnEnemyDeath called but all enemies already defeated!");
        return;
    }
    
    // Проверка: умирает именно текущий враг
    if (enemy != currentEnemy)
    {
        Debug.LogWarning($"⚠️ Wrong enemy died! Expected: {(currentEnemy != null ? currentEnemy.name : "null")}, Got: {enemy.name}");
        return;
    }
    
    // Проверка: враг ещё не уничтожен
    if (currentEnemyUI == null)
    {
        Debug.LogWarning("⚠️ OnEnemyDeath called but enemy UI already destroyed!");
        return;
    }
    
    // Увеличиваем счетчик ТОЛЬКО здесь и ТОЛЬКО один раз
    enemiesKilled = currentEnemyIndex + 1;
    
    SoundManager.Instance?.PlayEnemyDeathSound();
    
    Debug.Log($"💀 Enemy at index {currentEnemyIndex} defeated! Killed: {enemiesKilled}/{enemyStates.Length}");
    
    // Уничтожаем UI врага
    if (currentEnemyUI != null)
    {
        Destroy(currentEnemyUI);
        currentEnemyUI = null;
        currentEnemy = null;
    }
    
    // Переходим к следующему врагу
    currentEnemyIndex++;
    savedEnemyHealth = -1;
    
    // Останавливаем предыдущую корутину
    if (respawnCoroutine != null)
    {
        StopCoroutine(respawnCoroutine);
        respawnCoroutine = null;
    }
    
    // Проверяем, всех ли врагов убили
    if (currentEnemyIndex >= enemyStates.Length)
    {
        Debug.Log("🎉 ALL ENEMIES DEFEATED! Victory!");
        SetAllDefeated();
        ShowVictoryState();
        BackgroundManager.Instance?.OnEnemyChanged(currentEnemyIndex);
        return;
    }
    
    // Проверка на босса (последний враг)
    if (currentEnemyIndex == enemyStates.Length - 1)
    {
        SoundManager.Instance?.PlayBossMusic();
    }
    
    BackgroundManager.Instance?.OnEnemyChanged(currentEnemyIndex);
    
    // Сохраняем прогресс и обновляем UI
    SaveProgress();
    UpdateProgressBar();

    // Запускаем респавн с задержкой
    respawnCoroutine = StartCoroutine(RespawnEnemyWithDelay(0.8f));
    }
    
    private IEnumerator RespawnEnemyWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        respawnCoroutine = null;
        
        // Дополнительная проверка после задержки
        if (!allDefeated && currentEnemyIndex < enemyStates.Length)
        {
            // Проверяем, что враг ещё не создан
            if (currentEnemy == null && currentEnemyUI == null)
            {
                SpawnCurrentEnemy();
            }
            else
            {
                Debug.LogWarning("⚠️ Enemy already exists, skipping spawn");
            }
        }
        else
        {
            Debug.Log("⚠️ Respawn cancelled - all enemies defeated or invalid index");
        }
    }
    
    public EnemyState GetCurrentEnemyState()
    {
        return currentEnemy != null ? currentEnemy.state : null;
    }
    
    public string GetEnemyProgressText()
    {
        if (allDefeated)
            return "All enemies defeated!";
            
        return $"Enemy {currentEnemyIndex + 1}/{enemyStates.Length}";
    }
    
    public int GetEnemiesKilled() => enemiesKilled;
    public int GetCurrentEnemyIndex() => currentEnemyIndex;
    public int GetTotalEnemiesCount() => enemyStates?.Length ?? 0;
    
    public bool AllEnemiesDefeated()
    {
        return allDefeated;
    }
}