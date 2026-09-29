using UnityEngine;
using System.Collections.Generic;

public class Merchant : MonoBehaviour, IInteractable
{
    [SerializeField] private Inventory playerInventory;

    public string GetDisplayName()
    {
        return "Sell Goods";
    }

    public void Interact()
    {
        Dictionary<ItemData, int> snapshot = playerInventory.GetAllItems();

        foreach (KeyValuePair<ItemData, int> entry in snapshot)
        {
            ItemData item = entry.Key;
            int quantity = entry.Value;
            if (quantity > 0)
            {
                int totalPrice = item.itemPrice * quantity;
                GameManager.Instance.coins += totalPrice;
                playerInventory.RemoveItem(item, quantity);

                Debug.Log($"Sold {quantity} x {item.displayName} for {totalPrice} coins.");
            }
        }
        Debug.Log($"Coins: {GameManager.Instance.coins}");
    }
}