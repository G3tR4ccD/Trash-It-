using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SellTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ShopManager shopManager;
    public TextMeshProUGUI tooltipText;
    public GameObject shopPanel;

    private bool isHovering = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        tooltipText.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        tooltipText.gameObject.SetActive(false);
    }

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

    private void OnDisable()
    {
        isHovering = false;
    }
}