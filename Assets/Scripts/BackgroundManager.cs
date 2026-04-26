using UnityEngine;
using UnityEngine.UI;

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }
    
    [Header("Background Sprites")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite tier1Background; // Лес (враги 1-4)
    [SerializeField] private Sprite tier2Background; // Подземелье (враги 5-8)
    [SerializeField] private Sprite tier3Background; // Цитадель (враги 9-12)
    [SerializeField] private Sprite tier4Background; // Босс-арена (враги 13-16)
    [SerializeField] private Sprite victoryBackground; // После победы над всеми
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;
    
    private CanvasGroup canvasGroup;
    private Sprite currentBackground;
    private int currentTier = 1;
    
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
        
        canvasGroup = backgroundImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = backgroundImage.gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    private void Start()
    {
        // НЕ вызываем SetBackgroundByTier здесь, 
        // потому что EnemyManager ещё не загрузил прогресс
        // Вместо этого ждём вызова из EnemyManager
    }
    
    /// <summary>
    /// Вызывается из EnemyManager после загрузки прогресса
    /// </summary>
    public void InitializeBackground(int enemyIndex, bool allDefeated)
    {
        int tier = GetTierFromIndex(enemyIndex, allDefeated);
        SetBackgroundImmediate(tier);
    }
    
    private int GetTierFromIndex(int enemyIndex, bool allDefeated)
    {
        if (allDefeated)
            return 5; // Victory
        
        if (enemyIndex < 4) return 1;      // Враги 0-3: Tier 1
        if (enemyIndex < 8) return 2;      // Враги 4-7: Tier 2
        if (enemyIndex < 12) return 3;     // Враги 8-11: Tier 3
        return 4;                           // Враги 12-15: Tier 4
    }
    
    /// <summary>
    /// Установить фон мгновенно (без анимации) - используется при загрузке
    /// </summary>
    private void SetBackgroundImmediate(int tier)
    {
        Sprite newBackground = GetSpriteForTier(tier);
        
        if (newBackground != null)
        {
            backgroundImage.sprite = newBackground;
            currentBackground = newBackground;
            currentTier = tier;
            canvasGroup.alpha = 1f;
            
            Debug.Log($"🖼️ Background set to tier {tier} (immediate)");
        }
    }
    
    /// <summary>
    /// Установить фон с анимацией - используется при смене врага во время игры
    /// </summary>
    public void SetBackgroundByTier(int tier)
    {
        Sprite newBackground = GetSpriteForTier(tier);
        
        if (newBackground != null && newBackground != currentBackground)
        {
            StartCoroutine(FadeToNewBackground(newBackground, tier));
        }
    }
    
    private Sprite GetSpriteForTier(int tier)
    {
        switch (tier)
        {
            case 1: return tier1Background;
            case 2: return tier2Background;
            case 3: return tier3Background;
            case 4: return tier4Background;
            case 5: return victoryBackground;
            default: return tier1Background;
        }
    }
    
    private System.Collections.IEnumerator FadeToNewBackground(Sprite newSprite, int newTier)
    {
        // Затемняем
        float elapsed = 0f;
        while (elapsed < fadeDuration / 2)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / (fadeDuration / 2));
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;
        
        // Меняем спрайт
        backgroundImage.sprite = newSprite;
        currentBackground = newSprite;
        currentTier = newTier;
        
        Debug.Log($"🖼️ Background changed to tier {newTier}");
        
        // Проявляем
        elapsed = 0f;
        while (elapsed < fadeDuration / 2)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / (fadeDuration / 2));
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
    
    // Вызывается при смене врага во время игры
    public void OnEnemyChanged(int newEnemyIndex)
    {
        if (EnemyManager.Instance == null) return;
        
        bool allDefeated = EnemyManager.Instance.AllEnemiesDefeated();
        int newTier = GetTierFromIndex(newEnemyIndex, allDefeated);
        
        if (newTier != currentTier)
        {
            SetBackgroundByTier(newTier);
        }
    }
}