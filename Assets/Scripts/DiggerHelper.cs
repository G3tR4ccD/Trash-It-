using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;

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
    public LootRoller lootRoller;
    public Inventory dropoffBin;
    public float dropRadius = 1.5f;
    public int maxEmptyDigs = 6;
    public float reach = 2.5f;
    public float sweepArc = 120f;      // total sweep angle in degrees
    public float sweepStep = 15f;      // degrees per dig

    public float standBack = 3f;      // how far in front of the face the helper stands
    private Vector3 workDirection;    // the direction the helper looks while working
    private float sweepAngle = 0f;
    private int sweepDir = 1;
    private int emptyDigs = 0;
    private Vector3 mountainPoint;
    private Vector3 workSpot;
    private Inventory load;               // the helper's own backpack
    private int stuckHops = 0;
    private float highestReached = -1000f;
    private float lastDigTime = 0f;
    private bool returningToBin = false;
    public float minMountainHeight = 2f;   // ground above this world height counts as mountain
    public float ringStep = 2f;
    public float maxSearchDistance = 80f;
    public int samplesPerRing = 24;
    private bool faceFound = false;
    private float nextSearchTime = 0f;
    public float rotationSpeed = 8f;
    public float digRotationSpeed = 15f;
    private static Dictionary<DiggerHelper, Vector3> claimedPoints = new Dictionary<DiggerHelper, Vector3>();
    public float minDiggerSeparation = 5f;
    float EffectiveRadius => digRadius + GameManager.Instance.helperDigRadiusBonus;
    float EffectiveCooldown => Mathf.Max(0.1f, digCooldown - GameManager.Instance.helperCooldownReduction);
    private int baseCapacity;
    private Animator animator;
    private bool nextPawRight = true;

    bool FindNearestMountainPoint(out Vector3 point)
    {
        for (float r = ringStep; r <= maxSearchDistance; r += ringStep)
        {
            for (int i = 0; i < samplesPerRing; i++)
            {
                float angle = (i / (float)samplesPerRing) * Mathf.PI * 2f;
                float x = transform.position.x + Mathf.Cos(angle) * r;
                float z = transform.position.z + Mathf.Sin(angle) * r;
                if (TryGetGroundHeight(x, z, out float y))
                {
                    if (y - heightOffset >= minMountainHeight)
                    {
                        Vector3 candidate = new Vector3(x, y, z);
                        if (!IsClaimedByOther(candidate))
                        {
                            point = candidate;
                            return true;
                        }
                    }
                }
            }
        }
        point = transform.position;
        return false;
    }

    bool IsClaimedByOther(Vector3 point)
    {
        foreach (var kvp in claimedPoints)
        {
            if (kvp.Key == this) continue;
            if (FlatDistance(kvp.Value, point) < minDiggerSeparation) return true;
        }
        return false;
    }

    float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector2 aFlat = new Vector2(a.x, a.z);
        Vector2 bFlat = new Vector2(b.x, b.z);
        return Vector2.Distance(aFlat, bFlat);
    }

    private void Awake()
    {
        load = GetComponent<Inventory>();
        baseCapacity = load.maxCapacity;
        animator = GetComponent<Animator>();

    }
    void Update()
    {
        if (target == null) return;

        load.maxCapacity = baseCapacity + GameManager.Instance.helperBackpackBonus;

        if (!faceFound && Time.time >= nextSearchTime)
        {
            nextSearchTime = Time.time + 1f;

            if (FindNearestMountainPoint(out Vector3 p))
            {
                faceFound = true;
                claimedPoints[this] = p;

                Vector3 toFace = p - transform.position;
                toFace.y = 0f;
                toFace.Normalize();

                workDirection = toFace;
                target.position = p - toFace * standBack;
            }
        }

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
                if (Time.time - lastDigTime >= EffectiveCooldown) DigHere();
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
                    if (Time.time - lastDigTime >= EffectiveCooldown)
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

        if (Time.time - lastDigTime >= EffectiveCooldown)
        {
            if (animator != null)
            {
                animator.SetTrigger(nextPawRight ? "DigRight" : "DigLeft");
                nextPawRight = !nextPawRight;
            }
            Vector3 digPoint = GetSweepPoint();

            int[] dug = mountainManager.DigAt(digPoint, EffectiveRadius, digStrength);

            int total = 0;
            foreach (int count in dug)
            {
                total += count;
            }

            if (total > 0)
            {
                Vector3 lookDirection = digPoint - transform.position;
                lookDirection.y = 0f;
                if (lookDirection.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection);
                }
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
                faceFound = false;
                emptyDigs = 0;
                claimedPoints.Remove(this);
            }

            Dictionary<ItemData, int> found = lootRoller.RollLoot(dug);
            foreach (var entry in found)
            {
                load.InsertItem(entry.Key, entry.Value);
            }
            lastDigTime = Time.time;
        }
    }

    public void Setup(MountainManager mountain, Inventory bin, Vector3 startPoint)
    {
        mountainManager = mountain;
        dropoffBin = bin;
        mountainPoint = startPoint;

        target = new GameObject("HelperMarker").transform;
        target.position = startPoint;
    }
    Vector3 GetSweepPoint()
    {
        Vector3 dir = Quaternion.AngleAxis(sweepAngle, Vector3.up) * transform.forward;
        Vector3 flat = transform.position + dir * reach;

        Vector3 point;
        if (TryGetGroundHeight(flat.x, flat.z, out float y))
        {
            point = new Vector3(flat.x, y - heightOffset - 0.5f, flat.z);
        }
        else
        {
            point = new Vector3(transform.position.x, transform.position.y - heightOffset - 0.5f, transform.position.z);
        }

        sweepAngle += sweepDir * sweepStep;
        if (sweepAngle > sweepArc / 2f)
        {
            sweepAngle = sweepArc / 2f;
            sweepDir = -1;
        }
        else if (sweepAngle < -sweepArc / 2f)
        {
            sweepAngle = -sweepArc / 2f;
            sweepDir = 1;
        }

        return point;
    }
    private void OnDestroy()
    {
        claimedPoints.Remove(this);
    }
}