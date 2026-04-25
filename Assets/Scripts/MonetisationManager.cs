using UnityEngine;
using YG;

public class MonetisationManager : MonoBehaviour
{
    public static MonetisationManager Instance { get; private set; }
    
    [Header("Ad Settings")]
    [SerializeField] private float adCooldownMinutes = 5f;
    
    private bool isAdRemoved = false;
    private const string AD_REMOVED_KEY = "remove_ads_purchased";
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void OnEnable()
    {
        // Подписываемся на события покупок
        YG2.onPurchaseSuccess += OnPurchaseSuccess;
        
        // Подписываемся на события рекламы
        YG2.onCloseInterAdv += OnInterstitialClosed;
        YG2.onErrorInterAdv += OnInterstitialError;
    }
    
    private void OnDisable()
    {
        YG2.onPurchaseSuccess -= OnPurchaseSuccess;
        YG2.onCloseInterAdv -= OnInterstitialClosed;
        YG2.onErrorInterAdv -= OnInterstitialError;
    }
    
    private void Start()
    {
        // Консумируем необработанные покупки (важно!)
        YG2.ConsumePurchases();
        
        // Проверяем статус покупки
        CheckAdRemovalStatus();
    }
    
    // ========== ПОКУПКА ОТКЛЮЧЕНИЯ РЕКЛАМЫ ==========
    
    public void PurchaseAdRemoval()
    {
        YG2.BuyPayments("remove_ads");
    }
    
    private void OnPurchaseSuccess(string purchaseId)
    {
        if (purchaseId == "remove_ads")
        {
            RemoveAds();
        }
    }
    
    private void RemoveAds()
    {
        isAdRemoved = true;
        
        // Сохраняем локально
        PlayerPrefs.SetInt(AD_REMOVED_KEY, 1);
        PlayerPrefs.Save();
        
        Debug.Log("Ads removed permanently!");
    }
    
    private void CheckAdRemovalStatus()
    {
        // Проверяем через API покупок YG
        if (YG2.purchases != null)
        {
            foreach (var purchase in YG2.purchases)
            {
                if (purchase.id == "remove_ads" && purchase.consumed)
                {
                    isAdRemoved = true;
                    break;
                }
            }
        }
        
        // Дополнительно проверяем локальное сохранение
        if (PlayerPrefs.GetInt(AD_REMOVED_KEY, 0) == 1)
        {
            isAdRemoved = true;
        }
    }
    
    public bool IsAdRemoved()
    {
        return isAdRemoved;
    }
    
    // ========== ПОЛНОЭКРАННАЯ РЕКЛАМА ==========
    
    private bool CanShowInterstitial()
    {
        if (isAdRemoved)
        {
            Debug.Log("Ad not shown: removed by purchase");
            return false;
        }
        
        // Проверяем таймер интервала
        if (!YG2.isTimerAdvCompleted)
        {
            Debug.Log($"Ad not shown: cooldown active");
            return false;
        }
        
        return true;
    }
    
    public void TryShowInterstitial()
    {
        if (CanShowInterstitial())
        {
            Debug.Log("Showing interstitial ad...");
            YG2.InterstitialAdvShow();
        }
        else
        {
            Debug.Log("Interstitial skipped (cooldown or removed)");
        }
    }
    
    private void OnInterstitialClosed()
    {
        Debug.Log("Interstitial closed");
        
        // Возобновляем игру
        Time.timeScale = 1f;
        SoundManager.Instance?.ResumeMusic();
    }
    
    private void OnInterstitialError()
    {
        Debug.LogError("Interstitial ad error");
        
        // Возобновляем игру даже при ошибке
        Time.timeScale = 1f;
        SoundManager.Instance?.ResumeMusic();
    }
}