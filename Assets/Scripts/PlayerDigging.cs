using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDigging : MonoBehaviour
{
    public Camera playerCamera;
    public float digReach = 5f;
    public float digCooldown = 0.5f;
    public float digRadius = 1f;
    public Inventory inventory;

    public float digStrength = 200f;


    private bool isDigging = false;
    private float lastDigTime = 0f;

    [SerializeField] private ItemData item1;
    [SerializeField] private ItemData item2;


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
        if (isDigging && Time.time - lastDigTime >= digCooldown)
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(ray, out RaycastHit hit, digReach))
            {
                Chunk chunk = hit.collider.GetComponent<Chunk>();
                if (chunk != null)
                {
                    Vector3 localHitPoint = hit.point - hit.normal * 0.5f - chunk.transform.position;
                    int dug = chunk.DigSphere(localHitPoint, digRadius, digStrength);

                    lastDigTime = Time.time;

                    if (dug > 0)
                    {
                        GameManager.Instance.trashRemaining -= dug;
                        GiveLoot(dug);
                    }
                }
            }
        }
        

    }

    public void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    private void GiveLoot(int amount)
    {
        int item1Count = 0;
        int item2Count = 0;

        for (int i = 0; i < amount; i++)
        {
            if (Random.value < 0.6f)
            {
            item1Count++;
            }
            else
            {
            item2Count++;
            }
        }

        inventory.InsertItem(item1, item1Count);
        inventory.InsertItem(item2, item2Count);

        Debug.Log($"Dug {amount}. {item1.displayName}: {inventory.GetItemQuantity(item1)}, {item2.displayName}: {inventory.GetItemQuantity(item2)}, {GameManager.Instance.trashRemaining}");
    }


}
