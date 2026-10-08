using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class MachineSlot
{
    public UpgradeData upgrade;
    public GameObject machine;
}
public class ShopManager : MonoBehaviour, IInteractable
{
    private Dictionary<UpgradeData, int> purchaseCounts = new Dictionary<UpgradeData, int>();

    public Inventory playerInventory;
    public event System.Action OnShopOpened;
    public event System.Action OnShopClosed;
    public GameObject shopPanel;
    public GameObject tooltipText;
    public ItemDatabase itemDatabase;
    public MountainManager mountainManager;
    public GameObject[] pages;
    public List<UpgradeData> allUpgrades;
    public List<MachineSlot> machineSlots;

    public DiggerHelper diggerHelperPrefab;
    public CarrierHelper carrierHelperPrefab;
    public Transform helperSpawnPoint;   // an empty object on the floor near the bin
    public Transform helperStartPoint;   // an empty object over the mountain
    public Inventory dropoffBin;
    public Transform binStandPoint;
    public List<ItemRoute> itemRoutes;

    // page navigation methods
    public void OpenMainPage()
    {
        ShowPage(0);
    }
    public void OpenPlayerPage()
    {
        ShowPage(1);
    }
    public void OpenMachinePage()
    {
        ShowPage(2);
    }
    public void OpenHelperPage()
    {
        ShowPage(3);
    }
    // end page navigation methods

    public void SellWares()
    {
        SellInventory(playerInventory);
    }

    public void SellInventory(Inventory inventory)
    {
        Dictionary<ItemData, int> snapshot = inventory.GetAllItems();

        foreach (KeyValuePair<ItemData, int> entry in snapshot)
        {
            ItemData item = entry.Key;
            int quantity = entry.Value;
            if (quantity > 0)
            {
                int totalPrice = item.itemPrice * quantity;
                GameManager.Instance.coins += totalPrice;
                inventory.RemoveItem(item, quantity);
            }
        }
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
        if (upgrade.maxLevel > 0 && GetPurchaseCount(upgrade) >= upgrade.maxLevel)
        {
            return false;
        }
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
                GameManager.Instance.digRadiusBonus += 0.2f;
                break;
            case UpgradeData.UpgradeType.DigSpeed:
                GameManager.Instance.digCooldownReduction += 0.05f;
                break;
            case UpgradeData.UpgradeType.DigReach:
                GameManager.Instance.digReachBonus += 0.5f;
                break;
            case UpgradeData.UpgradeType.BackpackSize:
                playerInventory.IncreaseCapacity(10);
                break;
          case UpgradeData.UpgradeType.MachineSpeed:
                GameManager.Instance.machineSpeedMultiplier += 0.1f;
                break;
          case UpgradeData.UpgradeType.Machine:
            foreach (var slot in machineSlots)
            {
                if (slot.upgrade == upgrade && slot.machine != null)
                {
                    slot.machine.SetActive(true);
                    break;
                }
            }
            break;
          case UpgradeData.UpgradeType.DiggerHelper:
                DiggerHelper newHelper = Instantiate(diggerHelperPrefab, helperSpawnPoint.position, Quaternion.identity);
                newHelper.Setup(mountainManager, dropoffBin, helperStartPoint.position);
                dropoffBin.gameObject.SetActive(true);
                break;
            case UpgradeData.UpgradeType.CarrierHelper:
                CarrierHelper newCarrier = Instantiate(carrierHelperPrefab, helperSpawnPoint.position, Quaternion.identity);
                newCarrier.Setup(dropoffBin, binStandPoint, itemRoutes, this);
                dropoffBin.gameObject.SetActive(true);
                break;
            case UpgradeData.UpgradeType.HelperDigRadius:
                GameManager.Instance.helperDigRadiusBonus += 0.2f;
                break;
            case UpgradeData.UpgradeType.HelperDigSpeed:
                GameManager.Instance.helperCooldownReduction += 0.1f;
                break;
            case UpgradeData.UpgradeType.HelperBackpack:
                GameManager.Instance.helperBackpackBonus += 5;
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
        shopPanel.SetActive(true);
        ShowPage(0);
        OnShopOpened?.Invoke();
    }

    public void CloseShop()
    {
        ShowPage(-1);
        tooltipText.gameObject.SetActive(false);
        SaveGame();
        OnShopClosed?.Invoke();
    }

    public SaveData BuildSaveData()
    {
        SaveData data = new SaveData();
        data.coins = GameManager.Instance.coins;
        data.upgrades = new List<UpgradeSaveEntry>();

        data.trashRemaining = GameManager.Instance.trashRemaining;

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
    }
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
            UpgradeData upgrade = FindUpgradeByID(entry.upgradeID);

            if (upgrade == null)
            {
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
                continue;
            }

            playerInventory.InsertItem(item, entry.quantity);

        }
    }

    private UpgradeData FindUpgradeByID(string id)
    {
        foreach (var upgrade in allUpgrades)
        {
            if (upgrade.upgradeID == id)
            {
                return upgrade;
            }
        }
        return null;
    }

    public void Buy(UpgradeData upgrade)
    {
        TryPurchase(upgrade);
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

    public void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }
        tooltipText.gameObject.SetActive(false);
    }
}