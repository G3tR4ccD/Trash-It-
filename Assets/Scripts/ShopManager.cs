using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShopManager : MonoBehaviour, IInteractable
{
    private Dictionary<UpgradeData, int> purchaseCounts = new Dictionary<UpgradeData, int>();

    public PlayerDigging playerDigging;
    public Inventory playerInventory;
    public PlayerInput playerInput;
    public UpgradeData digRadiusUpgrade;
    public UpgradeData digSpeedUpgrade;
    public UpgradeData digReachUpgrade;
    public UpgradeData backpackUpgrade;
    public GameObject shopPanel;
    public GameObject tooltipText;
    public ItemDatabase itemDatabase;
    public MountainManager mountainManager;

    public void BuyDigRadius() 
    { 
        TryPurchase(digRadiusUpgrade);
    }
    public void BuyDigSpeed() 
    { 
        TryPurchase(digSpeedUpgrade); 
    }
    public void BuyDigReach() 
    { 
        TryPurchase(digReachUpgrade); 
    }
    public void BuyBackpack() 
    { 
        TryPurchase(backpackUpgrade); 
    }
    public void SellWares()
    {
        Dictionary<ItemData, int> snapshot = playerInventory.GetAllItems();

        foreach (KeyValuePair<ItemData, int> entry in snapshot)
        {
            ItemData item = entry.Key;
            int quantity = entry.Value;
            if (quantity > 0)
            {
                int totalPrice = item.itemPrice * quantity;
                GameManager.Instance.coins += totalPrice;
                playerInventory.RemoveItem(item, quantity);

                Debug.Log($"Sold {quantity} x {item.displayName} for {totalPrice} coins.");
            }
        }
        Debug.Log($"Coins: {GameManager.Instance.coins}");
    }

    public int GetSellValue()
    {
        Dictionary<ItemData, int> snapshot = playerInventory.GetAllItems();
        int total = 0;

        foreach (KeyValuePair<ItemData, int> entry in snapshot)
        {
            total += entry.Key.itemPrice * entry.Value;
        }

        return total;
    }

    public int GetPurchaseCount(UpgradeData upgrade)
    {
        if (purchaseCounts.TryGetValue(upgrade, out int count))
        {
            return count;
        }
        return 0;
    }

    public long GetCurrentCost(UpgradeData upgrade)
    {
        long baseCost = upgrade.baseCost;
        float multiplier = upgrade.multiplier;
        int purchaseCount = GetPurchaseCount(upgrade);
        return (long)(baseCost * Mathf.Pow(multiplier, purchaseCount));
    }

    public bool TryPurchase(UpgradeData upgrade)
    {
        long cost = GetCurrentCost(upgrade);
        if (GameManager.Instance.coins >= cost)
        {
            GameManager.Instance.coins -= cost;
            purchaseCounts[upgrade] = GetPurchaseCount(upgrade) + 1;
            ApplyUpgrade(upgrade);
            return true;
        }
        return false;
    }

    private void ApplyUpgrade(UpgradeData upgrade)
    {
        switch (upgrade.upgradeType)
        {
          case UpgradeData.UpgradeType.DigRadius:
            playerDigging.digRadius += 0.2f;
            break;
          case UpgradeData.UpgradeType.DigSpeed:
            playerDigging.digCooldown = Mathf.Max(0.05f, playerDigging.digCooldown - 0.05f);
            break;
          case UpgradeData.UpgradeType.DigReach:
            playerDigging.digReach += 0.5f; 
            break;
          case UpgradeData.UpgradeType.BackpackSize:
            playerInventory.IncreaseCapacity(10);
            break;
          case UpgradeData.UpgradeType.MachineSpeed:
            // this one's harder, let's hold off on it for now
            break; 
        }
    }
    public string GetDisplayName()
    {
        return "Shop";
    }
    public void Interact()
    {
        OpenShop();
    }

    public void OpenShop()
    {
        playerInput.SwitchCurrentActionMap("UI");

        Debug.Log("Opening shop");
        shopPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseShop()
    {
        playerInput.SwitchCurrentActionMap("Player");

        Debug.Log("Closing shop");
        shopPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        tooltipText.gameObject.SetActive(false);

        SaveGame();
    }

    public void OnCloseShop(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        CloseShop();
    }

    public SaveData BuildSaveData()
    {
        SaveData data = new SaveData();
        data.coins = GameManager.Instance.coins;
        data.upgrades = new List<UpgradeSaveEntry>();

        data.inventory = new List<ItemSaveEntry>();

        data.modifiedChunks = mountainManager.BuildChunkSaveData();

        foreach (var entry in playerInventory.GetAllItems())
        {
            ItemSaveEntry itemEntry = new ItemSaveEntry
            {
                itemID = entry.Key.itemID,
                quantity = entry.Value
            };
            data.inventory.Add(itemEntry);
        }

        foreach (var entry in purchaseCounts)
        {
            UpgradeSaveEntry upgradeEntry = new UpgradeSaveEntry
            {
                upgradeID = entry.Key.upgradeID,
                purchaseCount = entry.Value
            };
            data.upgrades.Add(upgradeEntry);
        }

        return data;
    }

    public void SaveGame()
    {
        SaveData data = BuildSaveData();
        string json = JsonUtility.ToJson(data);
        string path = Application.persistentDataPath + "/save.json";

        System.IO.File.WriteAllText(path, json);
        Debug.Log("Saved to " + path);
    }
    public void LoadGame()
    {
        string path = Application.persistentDataPath + "/save.json";

        if (!System.IO.File.Exists(path))
        {
            Debug.Log("No save file found.");
            return;
        }

        string json = System.IO.File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        GameManager.Instance.coins = data.coins;

        foreach (var entry in data.upgrades)
        {
            UpgradeData upgrade = FindUpgradeByID(entry.upgradeID);

            if (upgrade == null)
            {
                Debug.LogWarning("Could not find upgrade with ID: " + entry.upgradeID);
                continue;
            }

            purchaseCounts[upgrade] = entry.purchaseCount;

            for (int i = 0; i < entry.purchaseCount; i++)
            {
                ApplyUpgrade(upgrade);
            }
        }

        foreach (var entry in data.inventory)
        {
            ItemData item = itemDatabase.FindByID(entry.itemID);

            if (item == null)
            {
                Debug.LogWarning("Could not find item with ID: " + entry.itemID);
                continue;
            }

            playerInventory.InsertItem(item, entry.quantity);

        }
    }

    private UpgradeData FindUpgradeByID(string id)
    {
        if (digRadiusUpgrade.upgradeID == id) return digRadiusUpgrade;
        if (digSpeedUpgrade.upgradeID == id) return digSpeedUpgrade;
        if (digReachUpgrade.upgradeID == id) return digReachUpgrade;
        if (backpackUpgrade.upgradeID == id) return backpackUpgrade;
        return null;
    }

    void Start()
    {
        LoadGame();
        InvokeRepeating(nameof(SaveGame), 30f, 30f);
    }

    void OnApplicationQuit()
    {
        SaveGame();
    }
}