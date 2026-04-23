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
        
        // Получаем или добавляем CanvasGroup для плавных переходов
        canvasGroup = backgroundImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = backgroundImage.gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    private void Start()
    {
        // Устанавливаем начальный фон
        SetBackgroundByTier(GetCurrentTier());
    }
    
    private int GetCurrentTier()
    {
        if (EnemyManager.Instance == null) return 1;
        
        int enemyIndex = EnemyManager.Instance.GetCurrentEnemyIndex();
        
        if (EnemyManager.Instance.AllEnemiesDefeated())
            return 5; // Victory
        
        if (enemyIndex < 4) return 1;      // Враги 0-3: Tier 1
        if (enemyIndex < 8) return 2;      // Враги 4-7: Tier 2
        if (enemyIndex < 12) return 3;     // Враги 8-11: Tier 3
        return 4;                           // Враги 12-15: Tier 4
    }
    
    public void SetBackgroundByTier(int tier)
    {
        Sprite newBackground = null;
        
        switch (tier)
        {
            case 1:
                newBackground = tier1Background;
                break;
            case 2:
                newBackground = tier2Background;
                break;
            case 3:
                newBackground = tier3Background;
                break;
            case 4:
                newBackground = tier4Background;
                break;
            case 5:
                newBackground = victoryBackground;
                break;
        }
        
        if (newBackground != null && newBackground != currentBackground)
        {
            StartCoroutine(FadeToNewBackground(newBackground));
        }
    }
    
    private System.Collections.IEnumerator FadeToNewBackground(Sprite newSprite)
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
    
    // Вызывается при смене врага
    public void OnEnemyChanged(int newEnemyIndex)
    {
        int newTier = GetCurrentTier();
        SetBackgroundByTier(newTier);
    }
}