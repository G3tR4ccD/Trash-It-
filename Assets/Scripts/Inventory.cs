using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // start Serialized fields
    [Header("Capacity")]
    public int maxCapacity = 10;
    // end Serialized fields

    // start Events
    public event System.Action OnInventoryChanged;
    // end Events

    // start Runtime state
    private Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();
    // end Runtime state

    // start InsertItem
    public int InsertItem(ItemData item, int quantity)
    {
        if (quantity <= 0)
        {
            return 0;
        }

        int amountToAdd = Mathf.Min(quantity, GetFreeSpace());

        if (amountToAdd <= 0)
        {
            return 0;
        }

        if (items.ContainsKey(item))
        {
            items[item] += amountToAdd;
        }
        else
        {
            items[item] = amountToAdd;
        }

        OnInventoryChanged?.Invoke();
        return amountToAdd;
    }
    // end InsertItem

    // start RemoveItem
    public bool RemoveItem(ItemData item, int quantity)
    {
        if (quantity <= 0)
        {
            return false;
        }

        if (GetItemQuantity(item) < quantity)
        {
            return false;
        }

        items[item] -= quantity;

        if (items[item] <= 0)
        {
            items.Remove(item);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }
    // end RemoveItem

    // start GetItemQuantity
    public int GetItemQuantity(ItemData item)
    {
        if (items.TryGetValue(item, out int quantity))
        {
            return quantity;
        }
        return 0;
    }
    // end GetItemQuantity

    // start GetTotalItemCount
    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (int amount in items.Values)
        {
            total += amount;
        }
        return total;
    }
    // end GetTotalItemCount

    // start GetFreeSpace
    public int GetFreeSpace()
    {
        return maxCapacity - GetTotalItemCount();
    }
    // end GetFreeSpace

    // start IsFull
    public bool IsFull()
    {
        return GetTotalItemCount() >= maxCapacity;
    }
    // end IsFull

    // start GetAllItems
    public Dictionary<ItemData, int> GetAllItems()
    {
        return new Dictionary<ItemData, int>(items);
    }
    // end GetAllItems

    // start IncreaseCapacity
    public void IncreaseCapacity(int amount)
    {
        maxCapacity += amount;
        OnInventoryChanged?.Invoke();
    }
    // end IncreaseCapacity

    // start TransferAllTo
    public void TransferAllTo(Inventory other)
    {
        foreach (KeyValuePair<ItemData, int> entry in GetAllItems())
        {
            int transferredAmount = other.InsertItem(entry.Key, entry.Value);
            RemoveItem(entry.Key, transferredAmount);
        }
    }
    // end TransferAllTo
}