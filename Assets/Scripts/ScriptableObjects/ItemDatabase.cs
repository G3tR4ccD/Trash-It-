using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Scriptable Objects/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> allItems;

    public ItemData FindByID(string id)
    {
        foreach (ItemData item in allItems)
        {
            if (item.itemID == id)
            {
                return item;
            }
        }
        return null;
    }
}