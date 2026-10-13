using UnityEngine;

public class GameManager : MonoBehaviour
{
    // start Singleton
    public static GameManager Instance { get; private set; }
    // end Singleton

    // start Serialized fields
    [Header("Economy")]
    public long coins = 0;

    [Header("Trash")]
    public long trashRemaining = 1_000_000_000;
    public long bagsPerVoxel = 1;

    [Header("Machine Bonuses")]
    public float machineSpeedMultiplier = 1f;

    [Header("Helper Bonuses")]
    public float helperDigRadiusBonus = 0f;
    public float helperCooldownReduction = 0f;
    public int helperBackpackBonus = 0;

    [Header("Player Bonuses")]
    public float digRadiusBonus = 0f;
    public float digCooldownReduction = 0f;
    public float digReachBonus = 0f;
    // end Serialized fields

    // start Awake
    private void Awake()
    {
        Instance = this;
    }
    // end Awake
}