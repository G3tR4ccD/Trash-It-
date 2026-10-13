using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    // start Serialized fields
    [Header("References")]
    public ShopManager shopManager;
    public Inventory playerInventory;
    public MountainManager mountainManager;
    public ItemDatabase itemDatabase;
    public Transform player;
    public TrashPickup trashPickupPrefab;

    private Movement movement;

    [Header("Autosave")]
    public float autosaveInterval = 30f;
    // end Serialized fields

    // start Start
    void Start()
    {
        if (player == null) { Debug.LogError("SaveManager: player is not assigned"); return; }

        movement = player.GetComponent<Movement>();
        LoadGame();
        InvokeRepeating(nameof(SaveGame), autosaveInterval, autosaveInterval);
    }
    // end Start

    // start OnEnable
    void OnEnable()
    {
        shopManager.OnShopClosed += SaveGame;
    }
    // end OnEnable

    // start OnDisable
    void OnDisable()
    {
        shopManager.OnShopClosed -= SaveGame;
    }
    // end OnDisable

    // start OnApplicationQuit
    void OnApplicationQuit()
    {
        SaveGame();
    }
    // end OnApplicationQuit

    // start BuildSaveData
    public SaveData BuildSaveData()
    {
        SaveData data = new SaveData();
        data.coins = GameManager.Instance.coins;
        data.upgrades = new List<UpgradeSaveEntry>();

        data.trashRemaining = GameManager.Instance.trashRemaining;

        data.inventory = new List<ItemSaveEntry>();

        data.modifiedChunks = mountainManager.BuildChunkSaveData();

        data.hasPlayerPosition = true;
        data.playerPosition = player.position;

        data.playerYaw = movement.Yaw;
        data.playerPitch = movement.Pitch;

        data.storedInventories = new List<InventorySaveEntry>();

        data.storedMachines = new List<MachineSaveEntry>();

        data.droppedBags = new List<BagSaveEntry>();

        foreach (var entry in playerInventory.GetAllItems())
        {
            ItemSaveEntry itemEntry = new ItemSaveEntry
            {
                itemID = entry.Key.itemID,
                quantity = entry.Value
            };
            data.inventory.Add(itemEntry);
        }

        foreach (var entry in shopManager.GetAllPurchases())
        {
            UpgradeSaveEntry upgradeEntry = new UpgradeSaveEntry
            {
                upgradeID = entry.Key.upgradeID,
                purchaseCount = entry.Value
            };
            data.upgrades.Add(upgradeEntry);
        }

        foreach (SaveableInventory saveable in FindObjectsByType<SaveableInventory>(FindObjectsInactive.Include))
        {
            InventorySaveEntry entry = new InventorySaveEntry
            {
                inventoryID = saveable.inventoryID,
                items = new List<ItemSaveEntry>()
            };

            foreach (var itemPair in saveable.Inventory.GetAllItems())
            {
                ItemSaveEntry itemEntry = new ItemSaveEntry
                {
                    itemID = itemPair.Key.itemID,
                    quantity = itemPair.Value
                };
                entry.items.Add(itemEntry);
            }
            data.storedInventories.Add(entry);
        }

        foreach (Machine m in FindObjectsByType<Machine>(FindObjectsInactive.Include))
        {
            data.storedMachines.Add(m.GetSaveEntry());
        }

        foreach (TrashPickup bag in FindObjectsByType<TrashPickup>(FindObjectsInactive.Include))
        {
            BagSaveEntry bagEntry = new BagSaveEntry
            {
                itemID = bag.Item.itemID,
                amount = bag.Amount,
                position = bag.transform.position
            };
            data.droppedBags.Add(bagEntry);
        }

        return data;
    }
    // end BuildSaveData

    // start SaveGame
    public void SaveGame()
    {
        SaveData data = BuildSaveData();
        string json = JsonUtility.ToJson(data);
        string path = Application.persistentDataPath + "/save.json";

        System.IO.File.WriteAllText(path, json);
    }
    // end SaveGame

    // start LoadGame
    public void LoadGame()
    {
        string path = Application.persistentDataPath + "/save.json";

        if (!System.IO.File.Exists(path))
        {
            return;
        }

        string json = System.IO.File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        GameManager.Instance.coins = data.coins;
        GameManager.Instance.trashRemaining = data.trashRemaining;

        foreach (var entry in data.upgrades)
        {
            UpgradeData upgrade = shopManager.FindUpgradeByID(entry.upgradeID);

            if (upgrade == null)
            {
                continue;
            }

            shopManager.RestorePurchase(upgrade, entry.purchaseCount);
        }

        foreach (var entry in data.inventory)
        {
            ItemData item = itemDatabase.FindByID(entry.itemID);

            if (item == null)
            {
                continue;
            }

            playerInventory.InsertItem(item, entry.quantity);
        }
        if (data.storedInventories != null)      // old save files don't have this list
        {
            SaveableInventory[] saveables = FindObjectsByType<SaveableInventory>(FindObjectsInactive.Include);

            foreach (InventorySaveEntry entry in data.storedInventories)
            {
                SaveableInventory match = null;
                foreach (SaveableInventory candidate in saveables)
                {
                    if (candidate.inventoryID == entry.inventoryID)
                    {
                        match = candidate;
                        break;
                    }
                }
                if (match == null) continue;

                foreach (ItemSaveEntry saved in entry.items)
                {
                    ItemData item = itemDatabase.FindByID(saved.itemID);
                    if (item == null) continue;

                    match.Inventory.InsertItem(item, saved.quantity);
                }
            }
        }

        if (data.storedMachines != null) 
        {
            Machine[] machines = FindObjectsByType<Machine>(FindObjectsInactive.Include);

            foreach (MachineSaveEntry entry in data.storedMachines)
            {
                foreach (Machine candidate in machines)
                {
                    if (candidate.machineID == entry.machineID)
                    {
                        candidate.LoadFromEntry(entry);
                        break;
                    }
                }
            }
        }

        if (data.droppedBags != null)
        {
            foreach (BagSaveEntry entry in data.droppedBags)
            {
                ItemData item = itemDatabase.FindByID(entry.itemID);
                if (item == null) continue;

                TrashPickup bag = Instantiate(trashPickupPrefab, entry.position, Quaternion.identity);
                bag.Setup(item, entry.amount, playerInventory);
            }
        }

        if (data.hasPlayerPosition)
        {
            MovePlayer(data.playerPosition);
            movement.Yaw = data.playerYaw;
            movement.Pitch = data.playerPitch;
        }

    }
    // end LoadGame

    // start MovePlayer
    void MovePlayer(Vector3 position)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.position = position;
        controller.enabled = true;
    }
    // end MovePlayer
}