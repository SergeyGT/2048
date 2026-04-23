using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image enemyIcon;
    
    public EnemyState state { get; private set; }
    public int currentHealth { get; private set; }
    public bool isDead => currentHealth <= 0;
    
    private bool deathProcessed = false;
    
    public void Initialize(EnemyState enemyState)
    {
        state = enemyState;
        currentHealth = enemyState.maxHealth;
        deathProcessed = false;
        
        if (enemyIcon != null)
            enemyIcon.sprite = enemyState.icon;
            
        UpdateHealthBar();
    }
    
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, state.maxHealth);
        UpdateHealthBar();
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead || deathProcessed) return;
        
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthBar();
        
        if (isDead && !deathProcessed)
        {
            deathProcessed = true;
            OnDeath();
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = state.maxHealth;
            healthBar.value = currentHealth;
        }
    }
    
    private void OnDeath()
    {
        GameManager.Instance.OnEnemyKilled(state);
        EnemyManager.Instance.OnEnemyDeath(this);
    }
    
    public int GetDamageForMerge(int mergePower)
    {
        return Mathf.RoundToInt(mergePower * state.damageMultiplier);
    }
    
    private void OnDestroy()
    {
        // Если враг уничтожается через Destroy(), а не через смерть
        if (!deathProcessed && currentHealth > 0)
        {
            Debug.Log($"🗑️ Enemy {state?.enemyName} destroyed without dying");
        }
    }
}