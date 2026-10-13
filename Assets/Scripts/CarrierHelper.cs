using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

// start ItemRoute class
[System.Serializable]
public class ItemRoute
{
    public ItemData item;
    public Transform destinationMachine;
}
// end ItemRoute class

// start CarrierState enum
public enum CarrierState
{
    GoingToBin,
    GoingToDestination,
    GoingToSell
}
// end CarrierState enum

public class CarrierHelper : MonoBehaviour
{
    // start Serialized fields
    [Header("References")]
    public Transform target;
    public Transform binStandPoint;
    public ShopManager shopManager;     // reference to the ShopManager for checking machine availability
    public Inventory dropoffBin;        // where diggers dump raw loot
    public Transform sellPoint;          // where the carrier goes to sell items

    [Header("Movement")]
    public LayerMask terrainMask;
    public float heightOffset = 1f;
    public float moveSpeed = 3f;
    public float arriveDistance = 0.3f;
    public float rotationSpeed = 8f;

    [Header("Routing")]
    public List<ItemRoute> itemRoutes;  // item -> the machine hopper it should go to next
    public float waitAtEmptyBin = 1f;   // pause before re-checking an empty bin
    public float maxWaitAtMachine = 6f;     // with the other Routing fields
    private float waitStart = 0f;           // with the runtime state
    // end Serialized fields

    // start Runtime state
    private Transform currentDestination;
    private Inventory load;             // the carrier's own backpack
    private CarrierState state = CarrierState.GoingToBin;
    private float lastBinCheckTime = -999f;
    private int baseCapacity;           // base capacity of the carrier's inventory
    private Animator animator;
    // end Runtime state

    // start Awake
    private void Awake()
    {
        load = GetComponent<Inventory>();
        baseCapacity = load.maxCapacity; // Store the base capacity
        animator = GetComponent<Animator>();
    }
    // end Awake

    // start Update
    void Update()
    {
        if (target == null) return;

        load.maxCapacity = baseCapacity + GameManager.Instance.helperBackpackBonus; // Update capacity based on GameManager

        if (TryGetGroundHeight(transform.position.x, transform.position.z, out float myGround))
        {
            transform.position = new Vector3(transform.position.x, myGround, transform.position.z);
        }

        Vector3 goal = new Vector3(target.position.x, transform.position.y, target.position.z);

        if (FlatDistance(transform.position, goal) < arriveDistance)
        {
            HandleArrival();
        }

        Vector3 next = Vector3.MoveTowards(transform.position, goal, moveSpeed * Time.deltaTime);
        if (TryGetGroundHeight(next.x, next.z, out float groundY))
        {
            next.y = groundY;
        }

        Vector3 direction = next - transform.position;
        direction.y = 0f;

        if (animator != null)
        {
            float speed = Time.deltaTime > 0f ? direction.magnitude / Time.deltaTime : 0f;
            animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        }

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed);
        }

        transform.position = next;
    }
    // end Update

    // start HandleArrival
    void HandleArrival()
    {
        switch (state)
        {
            case CarrierState.GoingToBin:
                TryPickupFromBin();
                break;
            case CarrierState.GoingToDestination:
                DeliverToMachine();
                break;
            case CarrierState.GoingToSell:
                DeliverToSell();
                break;
        }
    }
    // end HandleArrival

    // start TryPickupFromBin
    void TryPickupFromBin()
    {
        if (Time.time - lastBinCheckTime < waitAtEmptyBin) return;
        lastBinCheckTime = Time.time;

        bool hasExistingItems = false;
        ItemData existingItemType = null;

        foreach (var loadEntry in load.GetAllItems())
        {
            if (loadEntry.Value > 0)
            {
                hasExistingItems = true;
                existingItemType = loadEntry.Key;
                break;
            }
        }

        foreach (var entry in dropoffBin.GetAllItems())
        {
            if (entry.Value <= 0) continue;

            ItemData item = entry.Key;
            int amountInBin = entry.Value;

            if (hasExistingItems && item != existingItemType) continue;

            int currentTotalCount = 0;
            foreach (var loadEntry in load.GetAllItems())
            {
                currentTotalCount += loadEntry.Value;
            }
            int availableSpace = load.maxCapacity - currentTotalCount;

            if (availableSpace <= 0) return;

            Transform destination = FindRouteFor(item);
            int roomAtDestination = int.MaxValue;            // selling has no limit
            if (destination != null)
            {
                Inventory hopper = destination.GetComponent<Inventory>();
                if (hopper == null) continue;                // broken route: skip it
                roomAtDestination = hopper.GetFreeSpace();
                if (roomAtDestination <= 0) continue;        // full: try another item type
            }

            int amountToTake = Mathf.Min(amountInBin, availableSpace, roomAtDestination);

            dropoffBin.RemoveItem(item, amountToTake);
            load.InsertItem(item, amountToTake);

            if (destination != null)
            {
                currentDestination = destination;
                target.position = destination.position;
                state = CarrierState.GoingToDestination;
            }
            else
            {
                target.position = sellPoint.position;
                state = CarrierState.GoingToSell;
            }

            return;
        }
    }
    // end TryPickupFromBin

    // start FindRouteFor
    Transform FindRouteFor(ItemData item)
    {
        foreach (var route in itemRoutes)
        {
            if (route.item == item)
            {
                if (route.destinationMachine != null && route.destinationMachine.gameObject.activeInHierarchy)
                {
                    return route.destinationMachine;
                }
                return null;
            }
        }
        return null;
    }
    // end FindRouteFor

    // start DeliverToMachine
    void DeliverToMachine()
    {
        Inventory hopper = currentDestination != null ? currentDestination.GetComponent<Inventory>() : null;

        if (hopper == null)
        {
            load.TransferAllTo(dropoffBin);
            target.position = binStandPoint.position;
            state = CarrierState.GoingToBin;
            return;
        }
        else
        {
            var currentLoad = new List<KeyValuePair<ItemData, int>>(load.GetAllItems());
            foreach (var entry in currentLoad)
            {
                if (entry.Value <= 0) continue;

                int inserted = hopper.InsertItem(entry.Key, entry.Value);
                load.RemoveItem(entry.Key, inserted);
            }
        }

        bool isLoadEmpty = true;
        foreach (var entry in load.GetAllItems())
        {
            if (entry.Value > 0)
            {
                isLoadEmpty = false;
                break;
            }
        }

        if (isLoadEmpty)
        {
            waitStart = 0f;
            target.position = binStandPoint.transform.position;
            state = CarrierState.GoingToBin;
        }
        else
        {
            if (waitStart == 0f)
            {
                waitStart = Time.time;
            }
            else if (Time.time - waitStart > maxWaitAtMachine)
            {
                load.TransferAllTo(dropoffBin);
                target.position = binStandPoint.position;
                state = CarrierState.GoingToBin;
                waitStart = 0f;
            }
        }
    }
    // end DeliverToMachine

    // start DeliverToSell
    void DeliverToSell()
    {
        shopManager.SellInventory(load);
        target.position = binStandPoint.transform.position;
        state = CarrierState.GoingToBin;
    }
    // end DeliverToSell

    // start TryGetGroundHeight
    bool TryGetGroundHeight(float x, float z, out float groundY)
    {
        Ray ray = new Ray(new Vector3(x, 100f, z), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainMask))
        {
            groundY = hit.point.y + heightOffset;
            return true;
        }
        groundY = 0f;
        return false;
    }
    // end TryGetGroundHeight

    // start FlatDistance
    float FlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
    // end FlatDistance

    // start Setup
    public void Setup(Inventory bin, Transform standPoint, List<ItemRoute> routes, ShopManager shop)
    {
        dropoffBin = bin;
        binStandPoint = standPoint;
        itemRoutes = routes;
        shopManager = shop;
        sellPoint = shopManager.sellPoint;

        target = new GameObject("CarrierMarker").transform;
        target.position = standPoint.position;
        state = CarrierState.GoingToBin;
    }
    // end Setup
}