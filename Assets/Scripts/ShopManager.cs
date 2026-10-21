using System.Collections.Generic;
using UnityEngine;

// start MachineSlot class
[System.Serializable]
public class MachineSlot
{
    public UpgradeData upgrade;
    public GameObject machine;
}
// end MachineSlot class

public class ShopManager : MonoBehaviour, IInteractable
{
    // start Serialized fields
    [Header("References")]
    public Inventory playerInventory;
    public MountainManager mountainManager;

    [Header("UI")]
    public GameObject shopPanel;
    public GameObject tooltipText;
    public GameObject[] pages;

    [Header("Upgrades & Machines")]
    public List<UpgradeData> allUpgrades;
    public List<MachineSlot> machineSlots;

    [Header("Helpers")]
    public DiggerHelper diggerHelperPrefab;
    public CarrierHelper carrierHelperPrefab;
    public Transform helperSpawnPoint;

    [Header("Logistics")]
    public Inventory dropoffBin;
    public Transform binStandPoint;
    public Transform sellPoint;

    public List<ItemRoute> itemRoutes;
    // end Serialized fields

    // start Events (not serialized, so no header)
    public event System.Action OnShopOpened;
    public event System.Action OnShopClosed;
    // end Events

    // start Runtime state
    private Dictionary<UpgradeData, int> purchaseCounts = new Dictionary<UpgradeData, int>();
    // end Runtime state

    // start OpenMainPage
    public void OpenMainPage()
    {
        ShowPage(0);
    }
    // end OpenMainPage

    // start OpenPlayerPage
    public void OpenPlayerPage()
    {
        ShowPage(1);
    }
    // end OpenPlayerPage

    // start OpenMachinePage
    public void OpenMachinePage()
    {
        ShowPage(2);
    }
    // end OpenMachinePage

    // start OpenHelperPage
    public void OpenHelperPage()
    {
        ShowPage(3);
    }
    // end OpenHelperPage

    // start SellWares
    public void SellWares()
    {
        SellInventory(playerInventory);
    }
    // end SellWares

    // start SellInventory
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
    // end SellInventory

    // start GetSellValue
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
    // end GetSellValue

    // start GetPurchaseCount
    public int GetPurchaseCount(UpgradeData upgrade)
    {
        if (purchaseCounts.TryGetValue(upgrade, out int count))
        {
            return count;
        }
        return 0;
    }
    // end GetPurchaseCount

    // start GetCurrentCost
    public long GetCurrentCost(UpgradeData upgrade)
    {
        long baseCost = upgrade.baseCost;
        float multiplier = upgrade.multiplier;
        int purchaseCount = GetPurchaseCount(upgrade);
        return (long)(baseCost * Mathf.Pow(multiplier, purchaseCount));
    }
    // end GetCurrentCost

    // start TryPurchase
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
    // end TryPurchase

    // start ApplyUpgrade
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
                newHelper.Setup(mountainManager, dropoffBin, binStandPoint);
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
    // end ApplyUpgrade

    // start GetDisplayName
    public string GetDisplayName()
    {
        return "Shop";
    }
    // end GetDisplayName

    // start Interact
    public void Interact()
    {
        OpenShop();
    }
    // end Interact

    // start OpenShop
    public void OpenShop()
    {
        shopPanel.SetActive(true);
        ShowPage(0);
        OnShopOpened?.Invoke();
    }
    // end OpenShop

    // start CloseShop
    public void CloseShop()
    {
        ShowPage(-1);
        tooltipText.gameObject.SetActive(false);
        OnShopClosed?.Invoke();
    }
    // end CloseShop

    // start FindUpgradeByID
    public UpgradeData FindUpgradeByID(string id)
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
    // end FindUpgradeByID

    // start GetAllPurchases
    public Dictionary<UpgradeData, int> GetAllPurchases()
    {
        return new Dictionary<UpgradeData, int>(purchaseCounts);
    }
    // end GetAllPurchases

    // start RestorePurchase
    public void RestorePurchase(UpgradeData upgrade, int count)
    {
        purchaseCounts[upgrade] = count;

        for (int i = 0; i < count; i++)
        {
            ApplyUpgrade(upgrade);
        }
    }
    // end RestorePurchase

    // start Buy
    public void Buy(UpgradeData upgrade)
    {
        TryPurchase(upgrade);
    }
    // end Buy

    // start ShowPage
    public void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }
        tooltipText.gameObject.SetActive(false);
    }
    // end ShowPage
}