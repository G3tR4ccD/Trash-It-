using UnityEngine;
using System.Collections.Generic;

// start LootItem class
[System.Serializable]
public class LootItem
{
    [Header("Entry")]
    public ItemData itemName;
    public int weight;
}
// end LootItem class

[CreateAssetMenu(fileName = "LootTable", menuName = "Scriptable Objects/LootTable")]
public class LootTable : ScriptableObject
{
    // start Serialized fields
    [Header("Loot")]
    public List<LootItem> lootItems;
    // end Serialized fields

    // start Roll
    public ItemData Roll()
    {
        int total = 0;
        foreach (LootItem entry in lootItems)
        {
            total += entry.weight;
        }

        int roll = Random.Range(0, total);

        foreach (LootItem entry in lootItems)
        {
            if (roll < entry.weight)
            {
                return entry.itemName;
            }
            roll -= entry.weight;
        }

        return lootItems[lootItems.Count - 1].itemName;
    }
    // end Roll
}