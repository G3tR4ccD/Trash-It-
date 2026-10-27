using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDigging : MonoBehaviour
{
    // start Serialized fields
    [Header("References")]
    public Camera playerCamera;
    public Inventory inventory;
    public MountainManager mountainManager;
    public LootRoller lootRoller;
    [SerializeField] private TrashPickup trashPickupPrefab;

    [Header("Dig Settings")]
    public float digReach = 5f;
    public float digCooldown = 0.5f;
    public float digRadius = 1f;
    public float digStrength = 200f;
    // end Serialized fields

    // start Effective values (include upgrade bonuses)
    float EffectiveRadius => digRadius + GameManager.Instance.digRadiusBonus;
    float EffectiveCooldown => Mathf.Max(0.05f, digCooldown - GameManager.Instance.digCooldownReduction);
    float EffectiveReach => digReach + GameManager.Instance.digReachBonus;
    // end Effective values

    // start Runtime state
    private bool isDigging = false;
    private float lastDigTime = 0f;
    private Animator animator;
    private bool nextPawRight = true;
    [SerializeField] private float impactFraction = 0.4f;   // how far into the swing the paw lands
    // end Runtime state

    // start OnDig
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
    // end OnDig

    // start Update
    private void Update()
    {
        if (inventory.IsFull() && isDigging)
        {
            isDigging = false;
            return;
        }

        if (isDigging && Time.time - lastDigTime >= EffectiveCooldown)
        {
            if (TryGetDigHit(out RaycastHit hit))     // only swing if there's something to hit
            {
                lastDigTime = Time.time;
                if (animator != null)
                {
                    animator.SetTrigger(nextPawRight ? "DigRight" : "DigLeft");
                    nextPawRight = !nextPawRight;
                }
                StartCoroutine(DigAfterDelay());
            }
        }
    }
    // end Update

    // start TryGetDigHit
    bool TryGetDigHit(out RaycastHit hit)
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        return Physics.Raycast(ray, out hit, EffectiveReach);
    }
    // end TryGetDigHit

    // start DigAfterDelay
    IEnumerator DigAfterDelay()
    {
        yield return new WaitForSeconds(EffectiveCooldown * impactFraction);

        if (TryGetDigHit(out RaycastHit hit))
        {
            Vector3 worldHitPoint = hit.point - hit.normal * 0.5f;
            Vector3 dropPosition = hit.point + hit.normal * 0.5f;

            int[] dug = mountainManager.DigAt(worldHitPoint, EffectiveRadius, digStrength);

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
    // end DigAfterDelay

    // start Awake
    public void Awake()
    {
        inventory = GetComponent<Inventory>();
        animator = GetComponent<Animator>();
    }
    // end Awake

    // start GiveLoot
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
    // end GiveLoot

    // start SpawnBag
    private void SpawnBag(ItemData item, int amount, Vector3 position)
    {
        TrashPickup bag = Instantiate(trashPickupPrefab, position, Quaternion.identity);
        bag.Setup(item, amount, inventory);
    }
    // end SpawnBag
}