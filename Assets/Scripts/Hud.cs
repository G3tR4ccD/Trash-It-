using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI backpackText;
    public Inventory playerInventory;
    public Image backpackFillImage;
    public TextMeshProUGUI trashText;

    void Update()
    {
        coinsText.text = $"{GameManager.Instance.coins}";
        trashText.text = $"{FormatNumber(GameManager.Instance.trashRemaining)}";
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

    }

    string FormatNumber(long number)
    {
        if (number >= 1_000_000_000)
        {
            return (number / 1_000_000_000f).ToString("0.0") + "B";
        }
        if (number >= 1_000_000)
        {
            return (number / 1_000_000f).ToString("0.0") + "M";
        }
        if (number >= 1_000)
        {
            return (number / 1_000f).ToString("0.0") + "K";
        }
        return number.ToString();
    }
}