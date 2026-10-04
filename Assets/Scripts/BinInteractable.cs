using UnityEngine;

public class BinInteractable : MonoBehaviour, IInteractable
{
    public Inventory playerInventory;   // drag the player in
    private Inventory bin;

    void Awake()
    {
        bin = GetComponent<Inventory>();
    }

    public void Interact()
    {
        bin.TransferAllTo(playerInventory);
    }

    public string GetDisplayName()
    {
        return $"Bin ({bin.GetTotalItemCount()})";
    }
}