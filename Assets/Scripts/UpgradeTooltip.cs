using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UpgradeTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // start Serialized fields
    [Header("References")]
    public ShopManager shopManager;
    public TextMeshProUGUI tooltipText;
    public GameObject shopPanel;

    [Header("Upgrade")]
    public UpgradeData upgrade;
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

            int currentLevel = shopManager.GetPurchaseCount(upgrade);
            bool isMaxed = upgrade.maxLevel > 0 && currentLevel >= upgrade.maxLevel;

            if (isMaxed)
            {
                tooltipText.text = $"{upgrade.displayName}: Maxed";
            }
            else if (upgrade.maxLevel == 1)
            {
                long cost = shopManager.GetCurrentCost(upgrade);
                tooltipText.text = $"{upgrade.displayName}: Unlock ({cost})";
            }
            else
            {
                long currentCost = shopManager.GetCurrentCost(upgrade);
                long nextCost = (long)(upgrade.baseCost * Mathf.Pow(upgrade.multiplier, currentLevel + 1));
                tooltipText.text = $"Lv: {currentLevel + 1} ({currentCost}) > Lv: {currentLevel + 2} ({nextCost})";
            }

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