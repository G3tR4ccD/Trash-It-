using UnityEngine;

public class TrashPickup : MonoBehaviour, IInteractable
{
    // start Serialized fields
    [Header("References")]
    [SerializeField] private TrashPickup trashPickupPrefab;
    // end Serialized fields

    // start Runtime state
    private ItemData item;
    public ItemData Item => item;
    private int amount;
    public int Amount => amount;
    private Inventory playerInventory;

    // end Runtime state

    // start Setup
    public void Setup(ItemData newItem, int newAmount, Inventory inventory)
    {
        item = newItem;
        amount = newAmount;
        playerInventory = inventory;
    }
    // end Setup

    // start Interact
    public void Interact()
    {
        int added = playerInventory.InsertItem(item, amount);
        amount -= added;

        if (amount <= 0)
        {
            Destroy(gameObject);
        }
    }
    // end Interact

    // start GetDisplayName
    public string GetDisplayName()
    {
        return $"{item.displayName} Bag ({amount})";
    }
    // end GetDisplayName
}