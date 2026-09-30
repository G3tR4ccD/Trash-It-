using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI backpackText;
    public Inventory playerInventory;
    public Image backpackFillImage;

    void Update()
    {
        coinsText.text = $"{GameManager.Instance.coins}";
    }

    void OnEnable()
    {
        playerInventory.OnInventoryChanged += UpdateBackpackDisplay;
        UpdateBackpackDisplay(); // Update display immediately when enabled
    }

    void OnDisable()
    {
        playerInventory.OnInventoryChanged -= UpdateBackpackDisplay;
    }

    void UpdateBackpackDisplay()
    {
        backpackText.text = $"Backpack: {playerInventory.GetTotalItemCount()}/{playerInventory.maxCapacity}";

        float fillAmount = (float)playerInventory.GetTotalItemCount() / playerInventory.maxCapacity;
        backpackFillImage.fillAmount = fillAmount;

        Debug.Log($"Fill updated: {playerInventory.GetTotalItemCount()}/{playerInventory.maxCapacity} = {fillAmount}");
    }
}