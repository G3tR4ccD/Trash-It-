using UnityEngine;

public class BinInteractable : MonoBehaviour, IInteractable
{
    // start Serialized fields
    [Header("References")]
    public Inventory playerInventory;   // drag the player in
    // end Serialized fields

    // start Runtime state
    private Inventory bin;
    // end Runtime state

    // start Awake
    void Awake()
    {
        bin = GetComponent<Inventory>();
    }
    // end Awake

    // start Interact
    public void Interact()
    {
        bin.TransferAllTo(playerInventory);
    }
    // end Interact

    // start GetDisplayName
    public string GetDisplayName()
    {
        return $"Bin ({bin.GetTotalItemCount()})";
    }
    // end GetDisplayName
}