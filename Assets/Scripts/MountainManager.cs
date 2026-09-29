using System.Collections.Generic;
using UnityEngine;

public class MountainManager : MonoBehaviour
{
    public Chunk chunkPrefab;
    public int gridWidth = 4;   // chunks across, x direction
    public int gridDepth = 4;   // chunks across, z direction
    public Texture2D heightMap;

    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    void Start()
    {
        int chunkSize = chunkPrefab.chunkSize;
        int totalGridSize = gridWidth * chunkSize;

        for (int gx = 0; gx < gridWidth; gx++)
        {
            for (int gz = 0; gz < gridDepth; gz++)
            {
                Chunk newChunk = Instantiate(chunkPrefab, new Vector3(gx * chunkSize, 0, gz * chunkSize), Quaternion.identity);
                newChunk.chunkCoord = new Vector2Int(gx, gz);
                newChunk.totalGridSize = totalGridSize;
                newChunk.heightMap = heightMap;
                newChunk.Initialize();
                chunks[new Vector2Int(gx, gz)] = newChunk;
            }
        }
        int totalSolidVoxels = 0;
        foreach (Chunk chunk in chunks.Values)
        {
            totalSolidVoxels += chunk.CountSolidVoxels();
        }

        Debug.Log($"Total solid voxels: {totalSolidVoxels}");
        GameManager.Instance.bagsPerVoxel = GameManager.Instance.trashRemaining / totalSolidVoxels;
    }
    public int DigAt(Vector3 worldPosition, float radius, float strength)
    {
        int totalDug = 0;
        int chunkSize = chunkPrefab.chunkSize;

        // figure out which chunk coordinates the dig sphere could overlap
        int minX = Mathf.FloorToInt((worldPosition.x - radius) / chunkSize);
        int maxX = Mathf.FloorToInt((worldPosition.x + radius) / chunkSize);
        int minZ = Mathf.FloorToInt((worldPosition.z - radius) / chunkSize);
        int maxZ = Mathf.FloorToInt((worldPosition.z + radius) / chunkSize);

        for (int gx = minX; gx <= maxX; gx++)
        {
            for (int gz = minZ; gz <= maxZ; gz++)
            {
                Vector2Int chunkCoord = new Vector2Int(gx, gz);
                if (chunks.TryGetValue(chunkCoord, out Chunk chunk))
                {
                    // Convert world position to local position in the chunk
                    Vector3 localPosition = worldPosition - chunk.transform.position;
                    int dug = chunk.DigSphere(localPosition, radius, strength);
                    totalDug += dug;
                }
            }
        }

        return totalDug;
    }
}