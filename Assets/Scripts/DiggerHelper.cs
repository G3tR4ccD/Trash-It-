using System.Collections.Generic;
using UnityEngine;

public class DiggerHelper : MonoBehaviour
{
    public LayerMask terrainMask;
    public float heightOffset = 1f;   // half the helper's height, so its feet touch the ground
    public Transform target;
    public float moveSpeed = 3f;
    public float arriveDistance = 0.3f;
    public float searchRadius = 6f;
    public float searchStep = 1f;
    public float maxClimb = 2f;
    public MountainManager mountainManager;
    public float digRadius = 1f;
    public float digStrength = 200f;
    public float digCooldown = 1f;
    public float minClimb = 0.5f; // minimum height difference to consider climbing
    public int maxStuckHops = 3;        // hops with no progress before it digs where it stands
    public PlayerDigging playerDigging;   // drag the player in via the Inspector, for the loot tables
    public Inventory dropoffBin;
    public float dropRadius = 1.5f;
    public int maxEmptyDigs = 6;
    private int emptyDigs = 0;

    private Vector3 mountainPoint;
    private Vector3 workSpot;
    private Inventory load;               // the helper's own backpack
    private int stuckHops = 0;
    private float highestReached = -1000f;
    private float lastDigTime = 0f;
    private bool returningToBin = false;

    float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector2 aFlat = new Vector2(a.x, a.z);
        Vector2 bFlat = new Vector2(b.x, b.z);
        return Vector2.Distance(aFlat, bFlat);
    }

    private void Awake()
    {
        load = GetComponent<Inventory>();
        if (load == null)
        {
            Debug.LogError("DiggerHelper requires an Inventory component.");
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

        if (returningToBin && FlatDistance(transform.position, dropoffBin.transform.position) < dropRadius)
        {
            returningToBin = false;
            load.TransferAllTo(dropoffBin);
            target.position = workSpot;
        }
        else if (Vector3.Distance(transform.position, goal) < arriveDistance)
        {
            Vector3 best = FindHighestNearby();

            if (Vector3.Distance(best, transform.position) < arriveDistance)
            {
                // on a peak
                if (Time.time - lastDigTime >= digCooldown) DigHere();
            }
            else
            {
                if (transform.position.y > highestReached + 0.1f)
                {
                    highestReached = transform.position.y;
                    stuckHops = 0;
                }
                else
                {
                    stuckHops++;
                }

                if (stuckHops >= maxStuckHops)
                {
                    if (Time.time - lastDigTime >= digCooldown)
                    {
                        DigHere();
                        stuckHops = 0;
                        highestReached = -1000f;
                    }
                }
                else
                {
                    target.position = best;
                }
            }

        }

        Vector3 next = Vector3.MoveTowards(transform.position, goal, moveSpeed * Time.deltaTime);

        if (TryGetGroundHeight(next.x, next.z, out float groundY))  
        {
            next.y = groundY;
        }

        transform.position = next;
    }

    bool TryGetGroundHeight(float x, float z, out float groundY)
    {
        Ray ray = new Ray(new Vector3(x, 100f, z), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainMask))
        {
            groundY = hit.point.y + heightOffset;
            return true;
        }
        else
        {
            groundY = 0f;
            return false;
        }
    }

    Vector3 FindHighestNearby()
    {
        Vector3 best = transform.position;

        for (float dx = -searchRadius; dx <= searchRadius; dx += searchStep)
        {
            for (float dz = -searchRadius; dz <= searchRadius; dz += searchStep)
            {
                float x = transform.position.x + dx;
                float z = transform.position.z + dz;

                if (TryGetGroundHeight(x, z, out float y))
                {
                    if (y > best.y && Mathf.Abs(y - transform.position.y) <= maxClimb)
                    {
                        best = new Vector3(x, y, z);
                    }
                }
            }
        }
        if (Mathf.Abs(best.y - transform.position.y) < minClimb)
        {
            best = transform.position;
        }
        return best;
    }

    void DigHere()
    {
        if (load.IsFull())
        {
            returningToBin = true;
            workSpot = transform.position;
            target.position = dropoffBin.transform.position;
            return;
        }

        if (Time.time - lastDigTime >= digCooldown)
        {
            Vector3 digPoint = new Vector3(transform.position.x, transform.position.y - heightOffset - 0.5f, transform.position.z);
            int[] dug = mountainManager.DigAt(digPoint, digRadius, digStrength);

            int total = 0;
            foreach (int count in dug)
            {
                total += count;
            }
            GameManager.Instance.trashRemaining -= total * GameManager.Instance.bagsPerVoxel;

            if (total == 0)
            {
                emptyDigs++;
            }
            else
            {
                emptyDigs = 0;
            }

            if (emptyDigs >= maxEmptyDigs)
            {
                target.position = mountainPoint;
                emptyDigs = 0;
            }

            Dictionary<ItemData, int> found = playerDigging.RollLoot(dug);
            foreach (var entry in found)
            {
                load.InsertItem(entry.Key, entry.Value);
            }
            lastDigTime = Time.time;
        }
    }

    public void Setup(MountainManager mountain, PlayerDigging digging, Inventory bin, Vector3 startPoint)
    {
        mountainManager = mountain;
        playerDigging = digging;
        dropoffBin = bin;
        mountainPoint = startPoint;

        target = new GameObject("HelperMarker").transform;
        target.position = startPoint;


    }
}