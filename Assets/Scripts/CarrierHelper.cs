using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ItemRoute
{
    public ItemData item;
    public Transform destinationMachine;
}

public enum CarrierState
{
    GoingToBin,
    GoingToDestination,
    GoingToSell
}

public class CarrierHelper : MonoBehaviour
{
    public LayerMask terrainMask;
    public float heightOffset = 1f;
    public Transform target;
    public float moveSpeed = 3f;
    public float arriveDistance = 0.3f;

    public Inventory dropoffBin;        // where diggers dump raw loot
    public List<ItemRoute> itemRoutes;  // item -> the machine hopper it should go to next
    public float waitAtEmptyBin = 1f;   // pause before re-checking an empty bin
    public ShopManager shopManager; // reference to the ShopManager for checking machine availability
    public Transform binStandPoint;

    private Transform currentDestination;
    private Inventory load;             // the carrier's own backpack
    private CarrierState state = CarrierState.GoingToBin;
    private float lastBinCheckTime = -999f;

    private void Awake()
    {
        load = GetComponent<Inventory>();
        if (load == null)
        {
            Debug.LogError("CarrierHelper requires an Inventory component.");
        }
    }

    void Update()
    {
        if (target == null) return;

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
        transform.position = next;
    }

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

    void TryPickupFromBin()
    {
        if (Time.time - lastBinCheckTime < waitAtEmptyBin) return;
        lastBinCheckTime = Time.time;

        foreach (var entry in dropoffBin.GetAllItems())
        {
            if (entry.Value <= 0) continue;

            ItemData item = entry.Key;
            int amount = entry.Value;

            dropoffBin.RemoveItem(item, amount);
            load.InsertItem(item, amount);

            Transform destination = FindRouteFor(item);

            if (destination != null)
            {
                currentDestination = destination;
                target.position = destination.position;
                state = CarrierState.GoingToDestination;
            }
            else
            {
                target.position = shopManager.transform.position;
                state = CarrierState.GoingToSell;
            }

            return;
        }
    }

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
                return null; // route exists, but the machine isn't bought/active yet
            }
        }
        return null; // no route at all = finished product
    }

    void DeliverToMachine()
    {
        Inventory hopper = currentDestination != null ? currentDestination.GetComponent<Inventory>() : null;

        if (hopper == null)
        {
            Debug.LogWarning("No Inventory component found on destination machine!");
        }
        else
        {
            foreach (var entry in load.GetAllItems())
            {
                int inserted = hopper.InsertItem(entry.Key, entry.Value);
                load.RemoveItem(entry.Key, inserted);
            }
        }

        target.position = dropoffBin.transform.position;
        state = CarrierState.GoingToBin;
    }

    void DeliverToSell()
    {
        shopManager.SellInventory(load);
        target.position = dropoffBin.transform.position;
        state = CarrierState.GoingToBin;
    }

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

    float FlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    public void Setup(Inventory bin, Transform standPoint, List<ItemRoute> routes, ShopManager shop)
    {
        dropoffBin = bin;
        binStandPoint = standPoint;
        itemRoutes = routes;
        shopManager = shop;

        target = new GameObject("CarrierMarker").transform;
        target.position = standPoint.position;
        state = CarrierState.GoingToBin;
    }
}
