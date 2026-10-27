using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    // start Serialized fields
    [Header("Text")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI backpackText;
    public TextMeshProUGUI trashText;

    [Header("Backpack")]
    public Inventory playerInventory;
    public Image backpackFillImage;
    public TextMeshProUGUI inventoryListText;
    // end Serialized fields

    // start Update
    void Update()
    {
        coinsText.text = $"{GameManager.Instance.coins}";
        trashText.text = $"{FormatNumber(GameManager.Instance.trashRemaining)}";
    }
    // end Update

    // start OnEnable
    void OnEnable()
    {
        playerInventory.OnInventoryChanged += UpdateBackpackDisplay;
        UpdateBackpackDisplay(); // Update display immediately when enabled
    }
    // end OnEnable

    // start OnDisable
    void OnDisable()
    {
        playerInventory.OnInventoryChanged -= UpdateBackpackDisplay;
    }
    // end OnDisable

    // start UpdateBackpackDisplay
    void UpdateBackpackDisplay()
    {
        backpackText.text = $"Backpack: {playerInventory.GetTotalItemCount()}/{playerInventory.maxCapacity}";

        float fillAmount = (float)playerInventory.GetTotalItemCount() / playerInventory.maxCapacity;
        backpackFillImage.fillAmount = fillAmount;

        if (inventoryListText != null)
        {
            string list = "";
            foreach (var entry in playerInventory.GetAllItems())
            {
                list += $"{entry.Value} {entry.Key.displayName}\n";
            }
            inventoryListText.text = list;
        }

    }
    // end UpdateBackpackDisplay

    // start FormatNumber
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
    // end FormatNumber
}