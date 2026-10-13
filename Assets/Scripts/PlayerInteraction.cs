using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    // start Serialized fields
    [Header("Interaction")]
    public float interactionDistance = 3f;

    [Header("References")]
    public Camera playerCamera;
    public TextMeshProUGUI interactionText;
    public InputActionReference interactAction;
    // end Serialized fields

    // start Runtime state
    private IInteractable currentTarget;
    // end Runtime state

    // start OnInteract
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (currentTarget == null) return;

            currentTarget.Interact();
        }
    }
    // end OnInteract

    // start Update
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
            interactionText.text = $"Press {interactAction.action.GetBindingDisplayString()} to interact with {currentTarget.GetDisplayName()}";
        }
        else
        {
            interactionText.text = "";
        }
    }
    // end Update
}