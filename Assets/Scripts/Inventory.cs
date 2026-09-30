using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public int maxCapacity = 10;
    public event System.Action OnInventoryChanged;

    private Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();

    public int InsertItem(ItemData item, int quantity)
    {
        if (quantity <= 0)
        {
            return 0;   // invalid quantity, nothing added
        }

        int amountToAdd = Mathf.Min(quantity, GetFreeSpace());

        if (amountToAdd <= 0)
        {
            return 0;   // pockets full
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
        return amountToAdd;   // how many actually went in
    }


    public bool RemoveItem(ItemData item, int quantity)
    {
        if (quantity <= 0)
        {
            return false;   // invalid quantity, so it failed
        }

        if (GetItemQuantity(item) < quantity)
        {
            return false;   // not enough, so it failed
        }

        items[item] -= quantity;

        if (items[item] <= 0)
        {
            items.Remove(item);
        }

        OnInventoryChanged?.Invoke();
        return true;   // it worked
    }

    public int GetItemQuantity(ItemData item)
    {
        if (items.TryGetValue(item, out int quantity))
        {
            return quantity;
        }
        return 0;
    }

    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (int amount in items.Values)
        {
            total += amount;
        }
        return total;
    }

    public int GetFreeSpace()
    {
        return maxCapacity - GetTotalItemCount();
    }

    public bool IsFull()
    {
        return GetTotalItemCount() >= maxCapacity;
    }

    public Dictionary<ItemData, int> GetAllItems()
    {
        return new Dictionary<ItemData, int>(items);
    }

}
