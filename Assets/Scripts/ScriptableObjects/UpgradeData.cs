using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeData", menuName = "Scriptable Objects/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    // start Serialized fields
    [Header("Identity")]
    public string displayName;
    public string upgradeID;

    [Header("Upgrade")]
    public UpgradeType upgradeType;
    public int maxLevel = 0;

    [Header("Cost")]
    public long baseCost;
    public float multiplier;
    // end Serialized fields

    // start UpgradeType enum
    public enum UpgradeType
    {
        DigRadius,
        DigSpeed,
        DigReach,
        MachineSpeed,
        BackpackSize,
        Machine,
        DiggerHelper,
        CarrierHelper,
        HelperDigRadius,
        HelperDigSpeed,
        HelperBackpack,
    }
    // end UpgradeType enum
}