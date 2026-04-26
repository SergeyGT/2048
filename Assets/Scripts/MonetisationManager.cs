using System.Collections;
using UnityEngine;
using YG;

public class MonetisationManager : MonoBehaviour
{
    public static MonetisationManager Instance { get; private set; }
    
    [Header("Ad Settings")]
    [SerializeField] private float adCooldownMinutes = 5f;
    
    [Header("Ad Warning UI")]
    [SerializeField] private GameObject adWarningPanel; // Панель с предупреждением "Реклама через 2 секунды..."
    [SerializeField] private float adWarningDuration = 2f;
    
    private bool isAdRemoved = false;
    private float lastAdShowTime = 0f;
    private bool isAdShowing = false;
    
    private const string AD_REMOVED_KEY = "remove_ads_purchased";
    private const string LAST_AD_TIME_KEY = "last_ad_time";
    
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
        YG2.onPurchaseSuccess += OnPurchaseSuccess;
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
        lastAdShowTime = PlayerPrefs.GetFloat(LAST_AD_TIME_KEY, 0f);
        YG2.ConsumePurchases();
        CheckAdRemovalStatus();
        
        // Скрываем панель предупреждения при старте
        if (adWarningPanel != null)
        {
            adWarningPanel.SetActive(false);
        }
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
        PlayerPrefs.SetInt(AD_REMOVED_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("Ads removed permanently!");
    }
    
    private void CheckAdRemovalStatus()
    {
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
        
        if (PlayerPrefs.GetInt(AD_REMOVED_KEY, 0) == 1)
        {
            isAdRemoved = true;
        }
        
        Debug.Log($"Ad removal status: {(isAdRemoved ? "PURCHASED" : "NOT PURCHASED")}");
    }
    
    public bool IsAdRemoved()
    {
        return isAdRemoved;
    }
    
    [ContextMenu("Reset Ad Status")]
    public void ResetAdStatus()
    {
        isAdRemoved = false;
        PlayerPrefs.DeleteKey(AD_REMOVED_KEY);
        PlayerPrefs.DeleteKey(LAST_AD_TIME_KEY);
        PlayerPrefs.Save();
        Debug.Log("🔄 Ad status RESET!");
    }
    
    // ========== РЕКЛАМА ПО ТАЙМЕРУ (каждые 5 минут, с предупреждением) ==========
    
    private bool CanShowTimedInterstitial()
    {
        if (isAdRemoved)
        {
            Debug.Log("❌ Ad blocked: purchased removal");
            return false;
        }
        
        float timeSinceLastAd = Time.time - lastAdShowTime;
        float cooldownSeconds = adCooldownMinutes * 60f;
        
        if (timeSinceLastAd < cooldownSeconds)
        {
            float remaining = cooldownSeconds - timeSinceLastAd;
            Debug.Log($"❌ Ad blocked: cooldown ({Mathf.Floor(remaining)}s remaining)");
            return false;
        }
        
        if (YG2.nowInterAdv || isAdShowing)
        {
            Debug.Log("❌ Ad blocked: already showing");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Реклама по таймеру - с предупреждением за 2 секунды
    /// </summary>
    public void TryShowTimedInterstitial()
    {
        Debug.Log("🔄 TryShowTimedInterstitial called");
        
        if (CanShowTimedInterstitial())
        {
            StartCoroutine(ShowAdWithWarning());
        }
    }
    
    private IEnumerator ShowAdWithWarning()
    {
        isAdShowing = true;
        
        // Показываем предупреждение
        if (adWarningPanel != null)
        {
            adWarningPanel.SetActive(true);
            Debug.Log($"⚠️ Ad warning shown for {adWarningDuration} seconds");
        }
        
        // Ждем 2 секунды (Realtime потому что игра на паузе)
        yield return new WaitForSecondsRealtime(adWarningDuration);
        
        // Скрываем предупреждение
        if (adWarningPanel != null)
        {
            adWarningPanel.SetActive(false);
        }
        
        // Показываем рекламу
        Debug.Log("✅ Showing timed interstitial ad");
        ShowInterstitial();
    }
    
    // ========== РЕКЛАМА ПРИ ПОРАЖЕНИИ (всегда, без кулдауна и предупреждения) ==========
    
    /// <summary>
    /// Реклама при поражении - сразу, без кулдауна, без предупреждения
    /// </summary>
    public void TryShowGameOverInterstitial()
    {
        Debug.Log("🔄 TryShowGameOverInterstitial called");
        
        if (isAdRemoved)
        {
            Debug.Log("❌ Game over ad blocked: purchased removal");
            return;
        }
        
        if (YG2.nowInterAdv || isAdShowing)
        {
            Debug.Log("❌ Game over ad blocked: already showing");
            return;
        }
        
        Debug.Log("✅ Showing game over interstitial ad NOW");
        ShowInterstitial();
    }
    
    private void ShowInterstitial()
    {
        lastAdShowTime = Time.time;
        PlayerPrefs.SetFloat(LAST_AD_TIME_KEY, lastAdShowTime);
        PlayerPrefs.Save();
        
        Time.timeScale = 0f;
        SoundManager.Instance?.PauseMusic();
        
        YG2.InterstitialAdvShow();
    }
    
    private void OnInterstitialClosed()
    {
        Debug.Log("📺 Interstitial closed - resuming game");
        isAdShowing = false;
        Time.timeScale = 1f;
        SoundManager.Instance?.ResumeMusic();
    }
    
    private void OnInterstitialError()
    {
        Debug.LogError("❌ Interstitial error - resuming game anyway");
        isAdShowing = false;
        Time.timeScale = 1f;
        SoundManager.Instance?.ResumeMusic();
    }
}