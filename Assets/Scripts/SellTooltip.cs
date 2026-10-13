using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SellTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // start Serialized fields
    [Header("References")]
    public ShopManager shopManager;
    public TextMeshProUGUI tooltipText;
    public GameObject shopPanel;
    // end Serialized fields

    // start Runtime state
    private bool isHovering = false;
    // end Runtime state

    // start OnPointerEnter
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        tooltipText.gameObject.SetActive(true);
    }
    // end OnPointerEnter

    // start OnPointerExit
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        tooltipText.gameObject.SetActive(false);
    }
    // end OnPointerExit

    // start Update
    private void Update()
    {
        if (isHovering)
        {
            tooltipText.gameObject.SetActive(true);

            long sellValue = (shopManager.GetSellValue());

            tooltipText.text = $"Sell for {sellValue}";

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            tooltipText.transform.position = mousePosition + new Vector2(20, 20);
        }
    }
    // end Update

    // start OnDisable
    private void OnDisable()
    {
        isHovering = false;
    }
    // end OnDisable
}