using UnityEngine;

public class ItemGiver : MonoBehaviour, IInteractable
{
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private ItemData item;
    [SerializeField] private int amount  = 1;

    public void Interact()
    {
        if (playerInventory != null && item != null)
        {
            playerInventory.InsertItem(item, amount);
            Debug.Log($"Gave {amount} of {item.displayName} to the player. Player now has {playerInventory.GetItemQuantity(item)} of this item.");
        }
        else
        {
            Debug.LogWarning("Player inventory or item is not assigned.");
        }
    }
}
