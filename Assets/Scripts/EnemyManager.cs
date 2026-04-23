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
        // Начинаем с первого врага
        currentEnemyIndex = 0;
        SpawnEnemyAtIndex(currentEnemyIndex);
    }
    
    private void SpawnEnemyAtIndex(int index)
    {
        if (enemyStates == null || enemyStates.Length == 0)
        {
            Debug.LogError("No enemy states assigned!");
            return;
        }
        
        // Зацикливаем индекс если вышли за пределы
        if (index >= enemyStates.Length)
        {
            index = 0;
            Debug.Log("All enemies defeated! Restarting from first enemy.");
        }
        
        currentEnemyIndex = index;
        EnemyState selectedState = enemyStates[currentEnemyIndex];
        
        SpawnEnemyUI(selectedState);
        UpdateEnemyDisplay(selectedState);
        
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
                Debug.Log(Application.isMobilePlatform);
                if (Application.isMobilePlatform) rect.anchoredPosition = pointMobile.localPosition;
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
            enemyPortraitImage.sprite = state.icon;
    }
    
    public void ProcessMergeDamage(int mergePower, Vector2Int position)
    {
        if (currentEnemy == null || currentEnemy.isDead) return;
        
        int damage = currentEnemy.GetDamageForMerge(mergePower);
        currentEnemy.TakeDamage(damage);
        
        SoundManager.Instance?.PlayDamageSound();
        
        ShowDamageFeedback(damage, position);
    }
    
    private void ShowDamageFeedback(int damage, Vector2Int position)
    {
        Debug.Log($"Dealt {damage} damage to {GetCurrentEnemyState().enemyName}!");
    }
    
    public void OnEnemyDeath(Enemy enemy)
    {
        enemiesKilled++;
        
        SoundManager.Instance?.PlayEnemyDeathSound();
        
        Debug.Log($"Enemy defeated! Total killed: {enemiesKilled}");
    
        // Берём следующего врага по списку
        currentEnemyIndex++;
    
        // Проверяем, всех ли врагов убили
        if (currentEnemyIndex >= enemyStates.Length)
        {
            Debug.Log("ALL ENEMIES DEFEATED! GAME OVER!");
            GameManager.Instance.GameOver(); // Заканчиваем игру
            return; // НЕ спавним нового врага
        }
        
        if (currentEnemyIndex == enemyStates.Length - 1)
        {
            SoundManager.Instance?.PlayBossMusic();
        }
    
        StartCoroutine(RespawnEnemyWithDelay(0.5f));
    }
    
    private IEnumerator RespawnEnemyWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnEnemyAtIndex(currentEnemyIndex);
    }
    
    public EnemyState GetCurrentEnemyState()
    {
        return currentEnemy != null ? currentEnemy.state : null;
    }
    
    public int GetEnemiesKilled()
    {
        return enemiesKilled;
    }
    
    public int GetCurrentEnemyIndex()
    {
        return currentEnemyIndex;
    }
    
    public int GetTotalEnemiesCount()
    {
        return enemyStates != null ? enemyStates.Length : 0;
    }
    
    public void ResetProgress()
    {
        enemiesKilled = 0;
        currentEnemyIndex = 0;
        SpawnEnemyAtIndex(0);
    }
}