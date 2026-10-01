using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeData", menuName = "Scriptable Objects/UpgradeData")]
public class UpgradeData  : ScriptableObject
{
    public string displayName;
    public long baseCost;
    public float multiplier;
    public UpgradeType upgradeType;
    public string upgradeID;
    public enum UpgradeType
    {
        DigRadius,
        DigSpeed,
        DigReach,
        MachineSpeed,
        BackpackSize
    }
}
