using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI backpackText;
    public Inventory playerInventory;

    void Update()
    {
        coinsText.text = $"Coins: {GameManager.Instance.coins}";
        backpackText.text = $"Backpack: {playerInventory.GetTotalItemCount()}/{playerInventory.maxCapacity}";
    }
}