using UnityEngine;
using YG;

[CreateAssetMenu(fileName = "EnemyState", menuName = "Game/Enemy State")]
public class EnemyState : ScriptableObject
{
    [Header("Names")]
    public string enemyNameRU;
    public string enemyNameEN;
    
    [Header("Stats")]
    public int maxHealth;
    public float damageMultiplier;
    public Sprite icon;
    public int scoreReward = 100;
    public int coinsReward = 10;
    
    /// <summary>
    /// Получить имя врага на текущем языке
    /// </summary>
    public string enemyName
    {
        get
        {
            // Проверяем текущий язык через поле YG2
            string currentLang = "";
            
            try
            {
                currentLang = YG2.lang; // В вашей версии язык хранится в YG2.lang
            }
            catch
            {
                currentLang = "ru";
            }
            
            if (string.IsNullOrEmpty(currentLang))
                currentLang = "ru";
            
            // Если английский и есть перевод - возвращаем его
            if (currentLang == "en" && !string.IsNullOrEmpty(enemyNameEN))
                return enemyNameEN;
            
            // По умолчанию русский
            return enemyNameRU;
        }
    }
}