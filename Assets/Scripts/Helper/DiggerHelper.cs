using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DiggerHelper : HelperBase
{
    // start Serialized fields
    [Header("References")]
    public MountainManager mountainManager;
    public LootRoller lootRoller;
    public Inventory dropoffBin;
    public LayerMask mountainMask;   // set to the Mountain layer in the Inspector

    [Header("Mountain Search")]
    public float standBack = 2f;      // how far in front of the face the helper stands
    public float minDiggerSeparation = 5f;

    [Header("Digging")]
    public float digRadius = 1f;
    public float digStrength = 200f;
    public float digCooldown = 1f;
    public float reach = 2.5f;
    public float digRotationSpeed = 15f;
    public float reachHeight = 4f;   // how high the digger can reach
    public float minDigY = 1f;       // ignore the base slab below this height
    // end Serialized fields

    // start Effective values (include upgrade bonuses)
    float EffectiveRadius => digRadius + GameManager.Instance.helperDigRadiusBonus;
    float EffectiveCooldown => Mathf.Max(0.1f, digCooldown - GameManager.Instance.helperCooldownReduction);
    // end Effective values

    // start Runtime state
    private static Dictionary<DiggerHelper, Vector3> claimedPoints = new Dictionary<DiggerHelper, Vector3>();
    private Vector3 workDirection;    // the direction the helper looks while working
    private Vector3 workSpot;
    private float lastDigTime = 0f;
    private float standAngle = 0f;   // which side of the mountain this digger is working
    private Transform binStandPoint;
    private Vector3 aimDirection;

    enum DiggerState 
    { 
        PickStandPoint, Walking, Digging, Returning 
    }
    private DiggerState state = DiggerState.PickStandPoint;
    // end Runtime state

    bool TryFindStandPoint(float angleDeg, out Vector3 stand)
    {
        Vector3 dir = Quaternion.Euler(0, angleDeg, 0) * Vector3.forward;
        Vector3 start = mountainManager.Center + dir * mountainManager.OuterRadius;
        start.y = 0.7f;

        if (Physics.Raycast(start, -dir, out RaycastHit hit, mountainManager.OuterRadius * 2f, mountainMask))
        {
            Vector3 candidate = hit.point + dir * standBack;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                stand = navHit.position;
                return true;
            }
        }
        stand = Vector3.zero;
        return false;
    }
 

    // start IsClaimedByOther
    bool IsClaimedByOther(Vector3 point)
    {
        foreach (var kvp in claimedPoints)
        {
            if (kvp.Key == this) continue;
            if (FlatDistance(kvp.Value, point) < minDiggerSeparation) return true;
        }
        return false;
    }
    // end IsClaimedByOther

    // start FlatDistance
    float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector2 aFlat = new Vector2(a.x, a.z);
        Vector2 bFlat = new Vector2(b.x, b.z);
        return Vector2.Distance(aFlat, bFlat);
    }
    // end FlatDistance

    // start Update
    protected override void Update()
    {
        base.Update();
        switch (state)
        {
            case DiggerState.PickStandPoint:
                if (TryFindStandPoint(standAngle, out Vector3 stand) && !IsClaimedByOther(stand))
                {
                    if (FlatDistance(stand, workSpot) < 1f)
                    {
                        standAngle += 15f;
                        break;
                    }
                    if (MoveTo(stand))
                    {
                        claimedPoints[this] = stand;
                        workSpot = stand;
                        workDirection = -(Quaternion.Euler(0, standAngle, 0) * Vector3.forward);
                        state = DiggerState.Walking;
                    }
                }
                else
                {
                    standAngle += 3f;
                }
                break;
            case DiggerState.Walking:
                if (HasArrived())
                {
                    aimDirection = workDirection;
                    state = DiggerState.Digging;
                }
                break;

            case DiggerState.Digging:
                if (load.IsFull())
                {
                    MoveTo(binStandPoint.position);
                    state = DiggerState.Returning;
                    break;
                }
                transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(aimDirection),
                Time.deltaTime * digRotationSpeed);
                DigHere();
                break;
            case DiggerState.Returning:
                if (HasArrived())
                {
                    load.TransferAllTo(dropoffBin);
                    if (load.GetTotalItemCount() == 0)
                    {
                        MoveTo(workSpot);
                        state = DiggerState.Walking;
                    }
                }
                break;
        }
    }
    // end Update

    // start DigHere
    void DigHere()
    {
        if (Time.time - lastDigTime >= EffectiveCooldown)
        {
            if (!mountainManager.TryFindSolid(transform.position, reach, reachHeight, minDigY, out Vector3 digPoint))
            {
                state = DiggerState.PickStandPoint;
                claimedPoints.Remove(this);
                return;
            }

            if (animator != null)
            {
                animator.SetTrigger("Dig");
            }

            int[] dug = mountainManager.DigAt(digPoint, EffectiveRadius, digStrength);

            int total = 0;
            foreach (int count in dug)
            {
                total += count;
            }

            Vector3 toTarget = digPoint - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                aimDirection = toTarget.normalized;
            }

            GameManager.Instance.trashRemaining -= total * GameManager.Instance.bagsPerVoxel;

            Dictionary<ItemData, int> found = lootRoller.RollLoot(dug);
            foreach (var entry in found)
            {
                load.InsertItem(entry.Key, entry.Value);
            }
            lastDigTime = Time.time;
        }
    }
    // end DigHere

    // start Setup
    public void Setup(MountainManager mountain, Inventory bin, Transform binStand)
    {
        mountainManager = mountain;
        Vector3 offset = transform.position - mountainManager.Center;
        standAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        dropoffBin = bin;
        binStandPoint = binStand;
    }
    // end Setup

    // start OnDestroy
    private void OnDestroy()
    {
        claimedPoints.Remove(this);
    }
    // end OnDestroy
}