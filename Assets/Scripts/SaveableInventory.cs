using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class SaveableInventory : MonoBehaviour
{
    public string inventoryID;
    public Inventory Inventory => GetComponent<Inventory>();
}
