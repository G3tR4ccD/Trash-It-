using UnityEngine;
using UnityEngine.InputSystem;

public class ShopInputSwitcher : MonoBehaviour
{
    // start Serialized fields
    [Header("References")]
    public ShopManager shop;
    // end Serialized fields

    // start Runtime state
    private PlayerInput playerInput;
    // end Runtime state

    // start Awake
    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }
    // end Awake

    // start OnEnable
    void OnEnable()
    {
        shop.OnShopOpened += EnterShop;
        shop.OnShopClosed += ExitShop;
    }
    // end OnEnable

    // start OnDisable
    void OnDisable()
    {
        shop.OnShopOpened -= EnterShop;
        shop.OnShopClosed -= ExitShop;
    }
    // end OnDisable

    // start EnterShop
    void EnterShop()
    {
        playerInput.SwitchCurrentActionMap("UI");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    // end EnterShop

    // start ExitShop
    void ExitShop()
    {
        playerInput.SwitchCurrentActionMap("Player");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    // end ExitShop

    // start OnCloseShop
    public void OnCloseShop(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        shop.CloseShop();
    }
    // end OnCloseShop
}