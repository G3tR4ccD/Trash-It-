using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public long coins = 0;
    public long trashRemaining = 1_000_000_000;
    public long bagsPerVoxel = 1;
    public float machineSpeedMultiplier = 1f;

    private void Awake()
    {
        Instance = this;
    }

}
