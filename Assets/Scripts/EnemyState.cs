using UnityEngine;

[CreateAssetMenu(menuName = "Enemy State")]
public class EnemyState : ScriptableObject
{
    public string enemyName;
    public int maxHealth;
    public Sprite icon;
    public Color healthBarColor = Color.red;
    public float damageMultiplier = 1f;
    public int scoreReward;
    
    [TextArea]
    public string description;
}