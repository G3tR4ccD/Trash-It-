using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Inventory))]
public class HelperBase : MonoBehaviour
{
    protected NavMeshAgent agent;
    protected Animator animator;
    protected Inventory load;
    private int baseCapacity;


    // start Awake
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        load = GetComponent<Inventory>();
        baseCapacity = load.maxCapacity;
        agent.avoidancePriority = Random.Range(30, 70);
    }
    // end Awake

    // start Update
    protected virtual void Update()
    {
        load.maxCapacity = baseCapacity + GameManager.Instance.helperBackpackBonus;

        if (animator != null)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        }
    }
    // end Update


    // start MoveTo
    protected bool MoveTo(Vector3 point)
    {
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            return true;
        }
        return false;
        
    }
    // end MoveTo

    // start HasArrived
   protected bool HasArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }
    // end HasArrived
}
