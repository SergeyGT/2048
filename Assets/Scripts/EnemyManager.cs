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
    
    private Enemy currentEnemy;
    private GameObject currentEnemyUI;
    private int currentEnemyIndex = 0;
    private int enemiesKilled = 0;
    private int savedEnemyHealth = -1; // -1 значит полное здоровье
    
    // Ключи для PlayerPrefs
    private const string ENEMY_INDEX_KEY = "EnemyIndex";
    private const string ENEMY_HEALTH_KEY = "EnemyHealth";
    private const string ENEMIES_KILLED_KEY = "EnemiesKilled";
    
    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    
    private void Start()
    {
        // Загружаем сохранённый прогресс
        LoadProgress();
        
        // Проверяем, не закончились ли враги
        if (currentEnemyIndex >= enemyStates.Length)
        {
            Debug.Log("All enemies already defeated!");
            if (enemyNameText != null)
                enemyNameText.text = "All Defeated!";
            if (enemyPortraitImage != null)
                enemyPortraitImage.gameObject.SetActive(false);
            return;
        }
        
        // Спавним текущего врага с сохранённым здоровьем
        SpawnCurrentEnemy();
    }
    
    #region Save/Load System
    
    private void SaveProgress()
    {
        PlayerPrefs.SetInt(ENEMY_INDEX_KEY, currentEnemyIndex);
        PlayerPrefs.SetInt(ENEMIES_KILLED_KEY, enemiesKilled);
        
        // Сохраняем текущее здоровье врага
        if (currentEnemy != null && !currentEnemy.isDead)
        {
            PlayerPrefs.SetInt(ENEMY_HEALTH_KEY, currentEnemy.currentHealth);
        }
        else
        {
            PlayerPrefs.SetInt(ENEMY_HEALTH_KEY, -1);
        }
        
        PlayerPrefs.Save();
        
        Debug.Log($"Progress saved: Enemy {currentEnemyIndex}, Killed: {enemiesKilled}, Health: {(currentEnemy != null ? currentEnemy.currentHealth : -1)}");
    }
    
    private void LoadProgress()
    {
        currentEnemyIndex = PlayerPrefs.GetInt(ENEMY_INDEX_KEY, 0);
        enemiesKilled = PlayerPrefs.GetInt(ENEMIES_KILLED_KEY, 0);
        savedEnemyHealth = PlayerPrefs.GetInt(ENEMY_HEALTH_KEY, -1);
        
        Debug.Log($"Progress loaded: Enemy {currentEnemyIndex}, Killed: {enemiesKilled}, Health: {savedEnemyHealth}");
    }
    
    #endregion
    
    #region Context Menu
    
    [ContextMenu("Reset Enemy Progress")]
    public void ResetProgress()
    {
        currentEnemyIndex = 0;
        enemiesKilled = 0;
        savedEnemyHealth = -1;
        
        // Чистим сохранения
        PlayerPrefs.DeleteKey(ENEMY_INDEX_KEY);
        PlayerPrefs.DeleteKey(ENEMY_HEALTH_KEY);
        PlayerPrefs.DeleteKey(ENEMIES_KILLED_KEY);
        PlayerPrefs.Save();
        
        // Уничтожаем текущего врага если есть
        if (currentEnemyUI != null)
            Destroy(currentEnemyUI);
        
        // Включаем отображение если было скрыто
        if (enemyPortraitImage != null)
            enemyPortraitImage.gameObject.SetActive(true);
            
        // Спавним первого врага
        SpawnCurrentEnemy();
        
        Debug.Log("Enemy progress RESET!");
    }
    
    [ContextMenu("Print Current Progress")]
    public void PrintProgress()
    {
        Debug.Log($"--- Enemy Progress ---");
        Debug.Log($"Current Index: {currentEnemyIndex}/{enemyStates.Length}");
        Debug.Log($"Enemies Killed: {enemiesKilled}");
        Debug.Log($"Saved Health: {savedEnemyHealth}");
        Debug.Log($"All Defeated: {AllEnemiesDefeated()}");
    }
    
    #endregion
    
    private void SpawnCurrentEnemy()
    {
        if (currentEnemyIndex >= enemyStates.Length)
        {
            Debug.Log("All enemies defeated! No more enemies to spawn.");
            if (enemyNameText != null)
                enemyNameText.text = "All Defeated!";
            if (enemyPortraitImage != null)
                enemyPortraitImage.gameObject.SetActive(false);
            return;
        }
        
        EnemyState selectedState = enemyStates[currentEnemyIndex];
        
        SpawnEnemyUI(selectedState);
        UpdateEnemyDisplay(selectedState);
        
        // Восстанавливаем сохранённое здоровье
        if (currentEnemy != null && savedEnemyHealth > 0)
        {
            currentEnemy.SetHealth(savedEnemyHealth);
            Debug.Log($"Restored enemy health: {savedEnemyHealth}/{selectedState.maxHealth}");
            savedEnemyHealth = -1; // Сбрасываем после восстановления
        }
        
        Debug.Log($"Spawned enemy: {selectedState.enemyName} (Index: {currentEnemyIndex})");
    }
    
    private void SpawnEnemyUI(EnemyState state)
    {
        if (currentEnemyUI != null)
            Destroy(currentEnemyUI);
            
        if (enemyUIPrefab != null && enemyUIContainer != null)
        {
            currentEnemyUI = Instantiate(enemyUIPrefab, enemyUIContainer);
            
            RectTransform rect = currentEnemyUI.GetComponent<RectTransform>();
            if (rect != null)
            {
                if (Application.isMobilePlatform) 
                    rect.anchoredPosition = pointMobile.localPosition;
            }
            
            currentEnemy = currentEnemyUI.GetComponent<Enemy>();
            
            if (currentEnemy != null)
                currentEnemy.Initialize(state);
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
    }
    
    public void ProcessMergeDamage(int mergePower, Vector2Int position)
    {
        if (currentEnemy == null || currentEnemy.isDead) 
        {
            // Если врагов нет, просто играем 2048 бесконечно
            return;
        }
        
        int damage = currentEnemy.GetDamageForMerge(mergePower);
        currentEnemy.TakeDamage(damage);
        
        // Сохраняем прогресс после каждого удара
        SaveProgress();
        
        SoundManager.Instance?.PlayDamageSound();
    }
    
    public void OnEnemyDeath(Enemy enemy)
    {
        enemiesKilled++;
        
        SoundManager.Instance?.PlayEnemyDeathSound();
        
        Debug.Log($"Enemy defeated! Total killed: {enemiesKilled}");
    
        // Берём следующего врага
        currentEnemyIndex++;
        savedEnemyHealth = -1; // Новый враг с полным здоровьем
        
        // Сохраняем прогресс
        SaveProgress();
    
        // Проверяем, всех ли врагов убили
        if (currentEnemyIndex >= enemyStates.Length)
        {
            Debug.Log("ALL ENEMIES DEFEATED! Final victory!");
            
            if (enemyNameText != null)
                enemyNameText.text = "VICTORY!";
            if (enemyPortraitImage != null)
                enemyPortraitImage.gameObject.SetActive(false);
                
            // Не вызываем GameOver, врагов больше нет
            return;
        }
        
        // Проверка на босса (последний враг)
        if (currentEnemyIndex == enemyStates.Length - 1)
        {
            SoundManager.Instance?.PlayBossMusic();
        }
    
        StartCoroutine(RespawnEnemyWithDelay(0.8f));
    }
    
    private IEnumerator RespawnEnemyWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnCurrentEnemy();
    }
    
    // Публичные геттеры для UI
    public EnemyState GetCurrentEnemyState()
    {
        return currentEnemy != null ? currentEnemy.state : null;
    }
    
    public string GetEnemyProgressText()
    {
        if (currentEnemyIndex >= enemyStates.Length)
            return "All enemies defeated!";
            
        return $"Enemy {currentEnemyIndex + 1}/{enemyStates.Length}";
    }
    
    public int GetEnemiesKilled() => enemiesKilled;
    public int GetCurrentEnemyIndex() => currentEnemyIndex;
    public int GetTotalEnemiesCount() => enemyStates?.Length ?? 0;
    
    public bool AllEnemiesDefeated()
    {
        return currentEnemyIndex >= enemyStates.Length;
    }
}