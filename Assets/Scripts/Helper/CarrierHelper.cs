using System.Collections.Generic;
using UnityEngine;

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
    GoingToSell, 
    WorkingMachine, 
    DeliveringProduct
}
// end CarrierState enum

public class CarrierHelper : HelperBase
{
    // start Serialized fields
    [Header("References")]
    public Transform binStandPoint;
    public ShopManager shopManager;     // reference to the ShopManager for checking machine availability
    public Inventory dropoffBin;        // where diggers dump raw loot
    public Transform sellPoint;          // where the carrier goes to sell items

    [Header("Routing")]
    public List<ItemRoute> itemRoutes;  // item -> the machine hopper it should go to next
    public float waitAtEmptyBin = 1f;   // pause before re-checking an empty bin
    public float maxWaitAtMachine = 6f;     // with the other Routing fields
    private float waitStart = 0f;           // with the runtime state

    // end Serialized fields

    // start Runtime state
    private Transform currentDestination;
    private CarrierState state = CarrierState.GoingToBin;
    private float lastBinCheckTime = -999f;
    private Machine currentMachine;
    private bool isWorker;
    // end Runtime state

    // start Update
    protected override void Update()
    {
        base.Update();
        if (HasArrived())
        {
            HandleArrival();
        }
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
            case CarrierState.WorkingMachine:
                WorkMachine();
                break;
            case CarrierState.DeliveringProduct:
                DeliverProduct();
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
            int roomAtDestination = int.MaxValue; 
            if (destination != null)
            {
                Inventory hopper = destination.GetComponent<Inventory>();
                if (hopper == null) continue;                
                roomAtDestination = hopper.GetFreeSpace();
                if (roomAtDestination <= 0) continue;       
            }

            int amountToTake = Mathf.Min(amountInBin, availableSpace, roomAtDestination);

            dropoffBin.RemoveItem(item, amountToTake);
            load.InsertItem(item, amountToTake);

            if (destination != null)
            {
                currentDestination = destination;
                currentMachine = destination.GetComponentInParent<Machine>();
                MoveTo(destination.position);
                state = CarrierState.GoingToDestination;
            }
            else
            {
                MoveTo(sellPoint.position);
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

        if (currentMachine != null && !isWorker && currentMachine.TryStaff(this))
        {
            isWorker = true;
        }

        if (hopper == null)
        {
            load.TransferAllTo(dropoffBin);
            ReturnToBin();
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
            if (isWorker)
            {
                state = CarrierState.WorkingMachine;
                agent.avoidancePriority = 0;
                if (animator != null)
                {
                    animator.SetBool("Working", true);
                }
            }
            else
            {
                ReturnToBin();
            }
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
                ReturnToBin();
                waitStart = 0f;
            }
        }
    }
    // end DeliverToMachine

    // start DeliverToSell
    void DeliverToSell()
    {
        shopManager.SellInventory(load);
        ReturnToBin();
    }
    // end DeliverToSell

    // start Setup
    public void Setup(Inventory bin, Transform standPoint, List<ItemRoute> routes, ShopManager shop)
    {
        dropoffBin = bin;
        binStandPoint = standPoint;
        itemRoutes = routes;
        shopManager = shop;
        sellPoint = shopManager.sellPoint;

        ReturnToBin();
    }
    // end Setup

    // start WorkMachine
    void WorkMachine()
    {
        if (currentMachine == null)
        {
            ReturnToBin();
            return;
        }

        if (currentMachine.HasOutput)
        {
            currentMachine.TakeOutput(load);
        }

        if (!currentMachine.HasWork)
        {
            ReturnToBin(CarrierState.DeliveringProduct);
        }
    }
    // end WorkMachine

    // start DeliverProduct
    void DeliverProduct()
    {
        load.TransferAllTo(dropoffBin);
        if (load.GetTotalItemCount() == 0)
        {
            state = CarrierState.GoingToBin;
            return;
        }

        SendLoadOn(null);   // the barrel is full, so sell what's left
    }
    // end DeliverProduct

    // start ReleaseMachine
    void ReleaseMachine()
    {
        if (isWorker && currentMachine != null)
        {
            currentMachine.Unstaff(this);
        }
        isWorker = false;
        agent.avoidancePriority = basePriority;
        if (animator != null)
        {
            animator.SetBool("Working", false);
        }
    }
    // end ReleaseMachine

    // start ReturnToBin
    void ReturnToBin(CarrierState next = CarrierState.GoingToBin)
    {
        ReleaseMachine();
        MoveTo(binStandPoint.position);
        state = next;
    }
    // end ReturnToBin

    // start SendLoadOn
    void SendLoadOn(Transform destination)
    {
        ReleaseMachine();
        if (destination != null)
        {
            currentDestination = destination;
            currentMachine = destination.GetComponentInParent<Machine>();
            MoveTo(destination.position);
            state = CarrierState.GoingToDestination;
        }
        else
        {
            MoveTo(sellPoint.position);
            state = CarrierState.GoingToSell;
        }
    }
    // end SendLoadOn
}