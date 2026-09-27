using UnityEngine;

public class TrashPickup : MonoBehaviour, IInteractable
{
    private ItemData item;
    private int amount;
    private Inventory playerInventory;

    [SerializeField] private TrashPickup trashPickupPrefab;

    // called by whoever spawns the bag, right after creating it
    public void Setup(ItemData newItem, int newAmount, Inventory inventory)
    {
        item = newItem;
        amount = newAmount;
        playerInventory = inventory;
    }

    public void Interact()
    {
        int added = playerInventory.InsertItem(item, amount);
        amount -= added;

        if (amount <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"Pockets full! {amount} {item.displayName} left in the bag.");
        }
    }
    public string GetDisplayName()
    {
        return $"{item.displayName} Bag ({amount})";
    }
}