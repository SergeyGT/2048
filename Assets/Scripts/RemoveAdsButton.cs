using UnityEngine;
using UnityEngine.UI;

public class RemoveAdsButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private GameObject purchasedIndicator;
    
    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
        
        UpdateUI();
    }
    
    private void OnEnable()
    {
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        bool isPurchased = MonetisationManager.Instance?.IsAdRemoved() ?? false;
        
        if (button != null)
        {
            button.interactable = !isPurchased;
        }
        
        if (purchasedIndicator != null)
        {
            purchasedIndicator.SetActive(isPurchased);
        }
    }
    
    private void OnClick()
    {
        MonetisationManager.Instance?.PurchaseAdRemoval();
    }
}