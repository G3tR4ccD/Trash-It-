using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public long coins = 0;
    public long trashRemaining = 1_000_000_000;

    private void Awake()
    {
        Instance = this;
    }

}
