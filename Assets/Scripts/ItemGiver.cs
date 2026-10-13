using UnityEngine;

public class ItemGiver : MonoBehaviour, IInteractable
{
    // start Serialized fields
    [Header("References")]
    [SerializeField] private Inventory playerInventory;

    [Header("Gift")]
    [SerializeField] private ItemData item;
    [SerializeField] private int amount = 1;
    // end Serialized fields

    // start Interact
    public void Interact()
    {
        if (playerInventory != null && item != null)
        {
            playerInventory.InsertItem(item, amount);
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