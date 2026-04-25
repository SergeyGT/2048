using UnityEngine;
using UnityEngine.UI;

public class ShopButton : MonoBehaviour
{
    [SerializeField] private Button removeAdsButton;
    [SerializeField] private GameObject adsRemovedIndicator; // Показывать если реклама отключена
    
    private void Start()
    {
        if (removeAdsButton != null)
        {
            removeAdsButton.onClick.AddListener(OnRemoveAdsClicked);
        }
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        bool adsRemoved = MonetisationManager.Instance?.IsAdRemoved() ?? false;
        
        if (removeAdsButton != null)
        {
            removeAdsButton.interactable = !adsRemoved;
        }
        
        if (adsRemovedIndicator != null)
        {
            adsRemovedIndicator.SetActive(adsRemoved);
        }
    }
    
    private void OnRemoveAdsClicked()
    {
        Debug.Log("Remove ads button clicked");
        MonetisationManager.Instance?.PurchaseAdRemoval();
    }
    
    private void OnEnable()
    {
        UpdateUI();
    }
}