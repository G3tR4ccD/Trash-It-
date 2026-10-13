using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Scriptable Objects/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    // start Serialized fields
    [Header("Items")]
    public List<ItemData> allItems;
    // end Serialized fields

    // start FindByID
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
    // end FindByID
}