using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public float interactionDistance = 3f;
    public Camera playerCamera;
    public TextMeshProUGUI interactionText;
    public InputActionReference interactAction;

    private IInteractable currentTarget;


    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (currentTarget == null) return;

            currentTarget.Interact();
        }
    }
    private void Update()
    {
        currentTarget = null; 

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            currentTarget = hit.collider.GetComponent<IInteractable>();
        }
        if (currentTarget != null)
        {
            interactionText.text = $"Press {interactAction.action.GetBindingDisplayString()} to interact with {hit.collider.gameObject.name}";
        }
        else
        {
            interactionText.text = "";
        }
    }



}
