using System.Collections.Generic;

[System.Serializable]
public class UpgradeSaveEntry
{
    public string upgradeID;
    public int purchaseCount;
}

[System.Serializable]
public class ItemSaveEntry
{
    public string itemID;
    public int quantity;
}

[System.Serializable]
public class ChunkSaveEntry
{
    public int chunkX;
    public int chunkZ;
    public byte[] voxelData;
}

[System.Serializable]
public class SaveData
{
    public long coins;
    public List<UpgradeSaveEntry> upgrades;
    public List<ItemSaveEntry> inventory;
    public List<ChunkSaveEntry> modifiedChunks;
    public long trashRemaining;
}


