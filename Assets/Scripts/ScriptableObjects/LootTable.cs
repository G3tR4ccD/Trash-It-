using UnityEngine;
using System.Collections.Generic;



[System.Serializable]
public class LootItem
{
    public ItemData itemName;
    public int weight; // Percentage chance of dropping this item
}
[CreateAssetMenu(fileName = "LootTable", menuName = "Scriptable Objects/LootTable")]
public class LootTable : ScriptableObject
{
    public List<LootItem> lootItems;

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

        return lootItems[lootItems.Count - 1].itemName;   // safety net, never normally reached
    }

}


