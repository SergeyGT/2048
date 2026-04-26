using UnityEngine;
using YG;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }
    
    [Header("Leaderboard")]
    [SerializeField] private string leaderboardName = "hiscore"; // Техническое название из консоли Яндекс.Игр
    
    private int bestScore = 0;
    private int coins = 0;
    
    private const string BEST_SCORE_KEY = "best_score";
    private const string COINS_KEY = "coins";
    
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
    
    private void Start()
    {
        LoadData();
    }
    
    // ========== ЛИДЕРБОРД ==========
    
    /// <summary>
    /// Отправить новый рекорд в таблицу лидеров.
    /// Таблица автоматически отображается на странице игры в Яндекс.Играх.
    /// </summary>
    public void SetLeaderboard(int newScore)
    {
        if (newScore > bestScore)
        {
            bestScore = newScore;
            
            // Сохраняем локально
            PlayerPrefs.SetInt(BEST_SCORE_KEY, bestScore);
            PlayerPrefs.Save();
            
            // Отправляем в лидерборд Яндекс.Игр
            YG2.SetLeaderboard(leaderboardName, bestScore);
            
            Debug.Log($"New record sent to leaderboard: {bestScore}");
        }
    }
    
    // ========== СОХРАНЕНИЯ ==========
    
    public void SaveCoins(int amount)
    {
        coins = amount;
        PlayerPrefs.SetInt(COINS_KEY, coins);
        PlayerPrefs.Save();
    }
    
    public void AddCoins(int amount)
    {
        coins += amount;
        SaveCoins(coins);
    }
    
    public int GetCoins()
    {
        return coins;
    }
    
    public int GetBestScore()
    {
        return bestScore;
    }
    
    private void LoadData()
    {
        bestScore = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);
        coins = PlayerPrefs.GetInt(COINS_KEY, 0);
        
        Debug.Log($"Data loaded. Best score: {bestScore}, Coins: {coins}");
    }
}