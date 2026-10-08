using UnityEngine;
using UnityEngine.InputSystem;

public class ShopInputSwitcher : MonoBehaviour
{
    public ShopManager shop;
    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    void OnEnable()
    {
        shop.OnShopOpened += EnterShop;
        shop.OnShopClosed += ExitShop;
    }

    void OnDisable()
    {
        shop.OnShopOpened -= EnterShop;
        shop.OnShopClosed -= ExitShop;
    }

    void EnterShop()
    {
        playerInput.SwitchCurrentActionMap("UI");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ExitShop()
    {
        playerInput.SwitchCurrentActionMap("Player");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnCloseShop(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        shop.CloseShop();
    }
}