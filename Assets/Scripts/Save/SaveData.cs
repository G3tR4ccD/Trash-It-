using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySaveEntry
{
    public string inventoryID;
    public List<ItemSaveEntry> items;
}

[System.Serializable]
public class BagSaveEntry
{
    public string itemID;
    public int amount;
    public Vector3 position;
}
// start UpgradeSaveEntry class
[System.Serializable]
public class UpgradeSaveEntry
{
    [Header("Upgrade")]
    public string upgradeID;
    public int purchaseCount;
}
// end UpgradeSaveEntry class

// start ItemSaveEntry class
[System.Serializable]
public class ItemSaveEntry
{
    [Header("Item")]
    public string itemID;
    public int quantity;
}
// end ItemSaveEntry class

// start ChunkSaveEntry class
[System.Serializable]
public class ChunkSaveEntry
{
    [Header("Position")]
    public int chunkX;
    public int chunkZ;

    [Header("Data")]
    public byte[] voxelData;
}
// end ChunkSaveEntry class

// start MachineSaveEntry
[System.Serializable]
public class MachineSaveEntry
{
    public string machineID;
    public int machineState;
    public float timeRemaining;
    public int pendingOutput;
    public int recipeIndex;
}
// end MachineSaveEntry

// start SaveData class
[System.Serializable]
public class SaveData
{
    [Header("Economy")]
    public long coins;

    [Header("Trash")]
    public long trashRemaining;

    [Header("Progress")]
    public List<UpgradeSaveEntry> upgrades;
    public List<ItemSaveEntry> inventory;

    [Header("World")]
    public List<ChunkSaveEntry> modifiedChunks;

    [Header("Player")]
    public bool hasPlayerPosition;
    public Vector3 playerPosition;
    public float playerYaw;
    public float playerPitch;

    [Header("Inventories")]
    public List<InventorySaveEntry> storedInventories;
    public List<BagSaveEntry> droppedBags;

    [Header("Machines")]
    public List<MachineSaveEntry> storedMachines;
}
// end SaveData class