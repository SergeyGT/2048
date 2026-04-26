using System.Collections;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private TileBoard board;
    [SerializeField] private CanvasGroup gameOver;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI hiscoreText;
    [SerializeField] private TextMeshProUGUI coinsText;
    
    [Header("Ad Timer")]
    [SerializeField] private float adIntervalMinutes = 5f;

    public int score { get; private set; } = 0;
    private bool isGameOver = false;
    private float lastAdTime;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject);
        } else {
            Instance = this;
        }
    }

    private void Start()
    {
        NewGame();
    }

    public void NewGame()
    {
        SetScore(0);
        
        int bestScore = LeaderboardManager.Instance?.GetBestScore() ?? 0;
        hiscoreText.text = bestScore.ToString();
        
        if (coinsText != null)
        {
            coinsText.text = LeaderboardManager.Instance?.GetCoins().ToString() ?? "0";
        }

        gameOver.alpha = 0f;
        gameOver.interactable = false;
        isGameOver = false;

        board.ClearBoard();
        board.CreateTile();
        board.CreateTile();
        board.enabled = true;
        
        lastAdTime = Time.time;
        
        SoundManager.Instance?.PlayBackgroundMusic();
        
        Debug.Log($"🆕 New Game started. Ad timer: {adIntervalMinutes} minutes");
    }

    private void Update()
    {
        if (!isGameOver)
        {
            float elapsed = Time.time - lastAdTime;
            if (elapsed >= adIntervalMinutes * 60f)
            {
                Debug.Log($"⏰ Ad timer reached: {elapsed}s elapsed");
                lastAdTime = Time.time;
                
                // Реклама по таймеру - с предупреждением
                MonetisationManager.Instance?.TryShowTimedInterstitial();
            }
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        board.enabled = false;
        gameOver.interactable = true;

        SoundManager.Instance?.PlayGameOverSound();
        SoundManager.Instance?.PlayGameOverMusic();
        
        LeaderboardManager.Instance?.SetLeaderboard(score);
        
        Debug.Log($"💀 Game Over! Score: {score}. Showing ad...");
        
        // Реклама при поражении - сразу, без кулдауна
        MonetisationManager.Instance?.TryShowGameOverInterstitial();
        
        StartCoroutine(Fade(gameOver, 1f, 1f));
    }

    private IEnumerator Fade(CanvasGroup canvasGroup, float to, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        float duration = 0.5f;
        float from = canvasGroup.alpha;

        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    public void IncreaseScore(int points)
    {
        SetScore(score + points);
        SoundManager.Instance?.PlayScoreIncrease();
    }

    private void SetScore(int newScore)
    {
        score = newScore;
        scoreText.text = score.ToString();
    }
    
    public void OnEnemyKilled(EnemyState enemyState)
    {
        if (enemyState != null)
        {
            IncreaseScore(enemyState.scoreReward);
        }
    }
}