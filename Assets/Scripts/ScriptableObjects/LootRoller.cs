using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootRoller", menuName = "Scriptable Objects/LootRoller")]
public class LootRoller : ScriptableObject
{
    public LootTable[] lootTables;

    public Dictionary<ItemData, int> RollLoot(int[] dugPerLayer)
    {
        Dictionary<ItemData, int> found = new Dictionary<ItemData, int>();

        for (int layer = 0; layer < dugPerLayer.Length; layer++)
        {
            for (int n = 0; n < dugPerLayer[layer]; n++)
            {
                ItemData item = lootTables[layer].Roll();
                if (item != null)
                {
                    if (!found.ContainsKey(item))
                    {
                        found[item] = 0;
                    }
                    found[item]++;
                }
            }
        }
        return found;
    }
}