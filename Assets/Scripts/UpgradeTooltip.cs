using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UpgradeTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ShopManager shopManager;
    public UpgradeData upgrade;
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

            int currentLevel = shopManager.GetPurchaseCount(upgrade) + 1;
            long currentCost = shopManager.GetCurrentCost(upgrade);
            long nextCost = (long)(upgrade.baseCost * Mathf.Pow(upgrade.multiplier, currentLevel));
            tooltipText.text = $"Lv: {currentLevel} ({currentCost}) > Lv: {currentLevel + 1} ({nextCost})";

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            tooltipText.transform.position = mousePosition + new Vector2(20, 20);
        }
    }
    private void OnDisable()
    {
        isHovering = false;
    }
}