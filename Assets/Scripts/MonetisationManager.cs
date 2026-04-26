using System.Collections;
using UnityEngine;
using YG;

public class MonetisationManager : MonoBehaviour
{
    public static MonetisationManager Instance { get; private set; }
    
    [Header("Ad Settings")]
    [SerializeField] private float adCooldownMinutes = 5f;
    
    [Header("Ad Warning UI")]
    [SerializeField] private GameObject adWarningPanel;
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
        
        if (adWarningPanel != null)
        {
            adWarningPanel.SetActive(false);
        }
        
        // ДИАГНОСТИКА
        Debug.Log($"📊 MonetisationManager STARTED. isAdRemoved={isAdRemoved}, lastAdShowTime={lastAdShowTime}");
    }
    
    // ========== ПОКУПКА ОТКЛЮЧЕНИЯ РЕКЛАМЫ ==========
    
    public void PurchaseAdRemoval()
    {
        Debug.Log("💳 PurchaseAdRemoval called");
        YG2.BuyPayments("remove_ads");
    }
    
    private void OnPurchaseSuccess(string purchaseId)
    {
        Debug.Log($"💰 Purchase success: {purchaseId}");
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
        Debug.Log("✅ Ads removed permanently!");
    }
    
    private void CheckAdRemovalStatus()
    {
        // ВСЕГДА проверяем локальное сохранение
        if (PlayerPrefs.GetInt(AD_REMOVED_KEY, 0) == 1)
        {
            isAdRemoved = true;
        }
        
        // НЕ проверяем YG2.purchases в Editor (это симуляция)
        #if !UNITY_EDITOR
        if (!isAdRemoved && YG2.purchases != null)
        {
            foreach (var purchase in YG2.purchases)
            {
                if (purchase.id == "remove_ads" && purchase.consumed)
                {
                    isAdRemoved = true;
                    PlayerPrefs.SetInt(AD_REMOVED_KEY, 1);
                    PlayerPrefs.Save();
                    break;
                }
            }
        }
        #endif
        
        Debug.Log($"📊 CheckAdRemovalStatus: isAdRemoved={isAdRemoved}");
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
        Debug.Log("🔄 Ad status RESET! isAdRemoved=false");
    }
    
    // ========== РЕКЛАМА ПО ТАЙМЕРУ ==========
    
    public void TryShowTimedInterstitial()
    {
        Debug.Log($"🔄 TryShowTimedInterstitial called. isAdRemoved={isAdRemoved}, isAdShowing={isAdShowing}, timeSinceLastAd={Time.time - lastAdShowTime}");
        
        if (isAdRemoved)
        {
            Debug.Log("❌ Timed ad blocked: purchased");
            return;
        }
        
        if (isAdShowing)
        {
            Debug.Log("❌ Timed ad blocked: already showing");
            return;
        }
        
        if (YG2.nowInterAdv)
        {
            Debug.Log("❌ Timed ad blocked: YG ad in progress");
            return;
        }
        
        float timeSinceLastAd = Time.time - lastAdShowTime;
        if (timeSinceLastAd < adCooldownMinutes * 60f)
        {
            Debug.Log($"❌ Timed ad blocked: cooldown ({Mathf.Floor(adCooldownMinutes * 60f - timeSinceLastAd)}s remaining)");
            return;
        }
        
        Debug.Log("✅ Starting timed ad with warning...");
        StartCoroutine(ShowAdWithWarning());
    }
    
    private IEnumerator ShowAdWithWarning()
    {
        isAdShowing = true;
        
        if (adWarningPanel != null)
        {
            adWarningPanel.SetActive(true);
            Debug.Log($"⚠️ Ad warning shown for {adWarningDuration} seconds");
        }
        
        yield return new WaitForSecondsRealtime(adWarningDuration);
        
        if (adWarningPanel != null)
        {
            adWarningPanel.SetActive(false);
        }
        
        Debug.Log("✅ Showing timed interstitial ad NOW");
        ShowInterstitial();
    }
    
    // ========== РЕКЛАМА ПРИ ПОРАЖЕНИИ (ВСЕГДА, кроме покупки) ==========
    
    public void TryShowGameOverInterstitial()
    {
        Debug.Log($"🔄 TryShowGameOverInterstitial called. isAdRemoved={isAdRemoved}, isAdShowing={isAdShowing}, YG2.nowInterAdv={YG2.nowInterAdv}");
        
        // ЕДИНСТВЕННАЯ проверка - покупка отключения
        if (isAdRemoved)
        {
            Debug.Log("❌ Game over ad blocked: purchased removal");
            return;
        }
        
        // Проверяем, не показывается ли уже реклама
        if (YG2.nowInterAdv)
        {
            Debug.Log("❌ Game over ad blocked: YG ad already showing");
            return;
        }
        
        if (isAdShowing)
        {
            Debug.Log("❌ Game over ad blocked: already in ad process");
            return;
        }
        
        // ВСЁ ОК - ПОКАЗЫВАЕМ РЕКЛАМУ СРАЗУ
        Debug.Log("✅✅✅ SHOWING GAME OVER AD NOW! ✅✅✅");
        ShowInterstitial();
    }
    
    private void ShowInterstitial()
    {
        isAdShowing = true;
        lastAdShowTime = Time.time;
        PlayerPrefs.SetFloat(LAST_AD_TIME_KEY, lastAdShowTime);
        PlayerPrefs.Save();
        
        Debug.Log($"📺 ShowInterstitial: Setting timeScale=0, calling YG2.InterstitialAdvShow()");
        
        Time.timeScale = 0f;
        SoundManager.Instance?.PauseMusic();
        
        YG2.InterstitialAdvShow();
    }
    
    private void OnInterstitialClosed()
    {
        Debug.Log("📺 OnInterstitialClosed - resuming game");
        isAdShowing = false;
        Time.timeScale = 1f;
        SoundManager.Instance?.ResumeMusic();
    }
    
    private void OnInterstitialError()
    {
        Debug.LogError("❌ OnInterstitialError - resuming game anyway");
        isAdShowing = false;
        Time.timeScale = 1f;
        SoundManager.Instance?.ResumeMusic();
    }
}