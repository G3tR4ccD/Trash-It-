using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerDigging : MonoBehaviour
{
    public Camera playerCamera;
    public float digReach = 5f;
    public float digCooldown = 0.5f;
    public float digRadius = 1f;
    public Inventory inventory;
    public MountainManager mountainManager;
    public float digStrength = 200f;
    public LootTable[] lootTables;


    private bool isDigging = false;
    private float lastDigTime = 0f;

    [SerializeField] private TrashPickup trashPickupPrefab;


    public void OnDig(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isDigging = true;
        }
        else if (context.canceled)
        {
            isDigging = false;
        }
    }

    private void Update()
    {
        if (inventory.IsFull() && isDigging)
        {
            Debug.Log("Pockets full!");
            isDigging = false;
            return;
        }

        if (isDigging && Time.time - lastDigTime >= digCooldown)
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(ray, out RaycastHit hit, digReach))
            {
                Vector3 worldHitPoint = hit.point - hit.normal * 0.5f;
                Vector3 dropPosition = hit.point + hit.normal * 0.5f;

                int[] dug = mountainManager.DigAt(worldHitPoint, digRadius, digStrength);
                lastDigTime = Time.time;

                int total = 0;
                foreach (int count in dug)
                {
                    total += count;
                }

                if (total > 0)
                {
                    GameManager.Instance.trashRemaining -= total * GameManager.Instance.bagsPerVoxel;
                    GiveLoot(dug, dropPosition);
                }
                else
                {
                    Debug.Log("Nothing dug.");
                }
                Debug.Log("Dug per layer: " + string.Join(", ", dug));
            }
        }
    }

    public void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    private void GiveLoot(int[] dugPerLayer, Vector3 dropPosition)
    {
        Dictionary<ItemData, int> found = new Dictionary<ItemData, int>();

        for (int layer = 0; layer < dugPerLayer.Length; layer++)
        {
            for (int n = 0; n < dugPerLayer[layer]; n++)
            {
                ItemData item = lootTables[layer].Roll();
                if (item != null)
                {
                    if (!found.ContainsKey(item))
                    {
                        found[item] = 0;
                    }
                    found[item]++;
                }
            }
        }

        foreach (KeyValuePair<ItemData, int> entry in found)
        {
            int amount = entry.Value;
            int added = inventory.InsertItem(entry.Key, amount);
            int leftover = amount - added;

            Debug.Log($"Added {added} of {entry.Key.displayName} to inventory.");

            if (leftover > 0)
            {
                SpawnBag(entry.Key, leftover, dropPosition);
            }
        }
    }

    private void SpawnBag(ItemData item, int amount, Vector3 position)
    {
        TrashPickup bag = Instantiate(trashPickupPrefab, position, Quaternion.identity);
        bag.Setup(item, amount, inventory);
    }
}