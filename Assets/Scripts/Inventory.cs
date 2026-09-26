using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();

    public void InsertItem(ItemData item, int quantity)
    {
        if (quantity <= 0)
        {
            return;   // invalid quantity, so it failed
        }

        if (items.ContainsKey(item))
        {
            items[item] += quantity;
        }
        else
        {
            items[item] = quantity;
        }
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
}
