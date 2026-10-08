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
    public LootRoller lootRoller;

    float EffectiveRadius => digRadius + GameManager.Instance.digRadiusBonus;
    float EffectiveCooldown => Mathf.Max(0.05f, digCooldown - GameManager.Instance.digCooldownReduction);
    float EffectiveReach => digReach + GameManager.Instance.digReachBonus;

    private bool isDigging = false;
    private float lastDigTime = 0f;
    private Animator animator;
    private bool nextPawRight = true;

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
            isDigging = false;
            return;
        }

        if (isDigging && Time.time - lastDigTime >= EffectiveCooldown)
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(ray, out RaycastHit hit, EffectiveReach))
            {
                Vector3 worldHitPoint = hit.point - hit.normal * 0.5f;
                Vector3 dropPosition = hit.point + hit.normal * 0.5f;

                int[] dug = mountainManager.DigAt(worldHitPoint, EffectiveRadius, digStrength);
                lastDigTime = Time.time;
                if (animator != null)
                {
                    animator.SetTrigger(nextPawRight ? "DigRight" : "DigLeft");
                    nextPawRight = !nextPawRight;
                }

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
            }
        }
    }

    public void Awake()
    {
        inventory = GetComponent<Inventory>();
        animator = GetComponent<Animator>();
    }

    private void GiveLoot(int[] dugPerLayer, Vector3 dropPosition)
    {
        Dictionary<ItemData, int> found = lootRoller.RollLoot(dugPerLayer);
        foreach (KeyValuePair<ItemData, int> entry in found)
        {
            int amount = entry.Value;
            int added = inventory.InsertItem(entry.Key, amount);
            int leftover = amount - added;

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