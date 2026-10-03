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

        Dictionary<Vector2Int, byte[]> savedChunks = LoadSavedChunkData();

        for (int gx = 0; gx < gridWidth; gx++)
        {
            for (int gz = 0; gz < gridDepth; gz++)
            {
                Chunk newChunk = Instantiate(chunkPrefab, new Vector3(gx * chunkSize, 0, gz * chunkSize), Quaternion.identity);
                newChunk.chunkCoord = new Vector2Int(gx, gz);
                newChunk.totalGridSize = totalGridSize;
                newChunk.heightMap = heightMap;

                Vector2Int coord = new Vector2Int(gx, gz);

                if (savedChunks.TryGetValue(coord, out byte[] voxelData))
                {
                    newChunk.InitializeFromSave(voxelData);
                }
                else
                {
                    newChunk.Initialize();
                }

                chunks[coord] = newChunk;
            }
        }

        int totalSolidVoxels = 0;
        foreach (Chunk chunk in chunks.Values)
        {
            totalSolidVoxels += chunk.CountSolidVoxels();
        }

        GameManager.Instance.bagsPerVoxel = GameManager.Instance.trashRemaining / totalSolidVoxels;
    }

    public int[] DigAt(Vector3 worldPosition, float radius, float strength)
    {
        int[] totalDug = new int[chunkPrefab.layerhardness.Length];
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
                    int[] dug = chunk.DigSphere(localPosition, radius, strength);
                    for (int i = 0; i < totalDug.Length; i++)
                    {
                        totalDug[i] += dug[i];
                    }
                }
            }
        }

        return totalDug;
    }

    public List<ChunkSaveEntry> BuildChunkSaveData()
    {
        List<ChunkSaveEntry> result = new List<ChunkSaveEntry>();

        foreach (var pair in chunks)
        {
            Chunk chunk = pair.Value;

            if (chunk.isModified)
            {
                ChunkSaveEntry entry = new ChunkSaveEntry
                {
                    chunkX = pair.Key.x,
                    chunkZ = pair.Key.y,
                    voxelData = chunk.GetVoxelBytes()
                };
                result.Add(entry);
            }
        }

        return result;
    }

    private Dictionary<Vector2Int, byte[]> LoadSavedChunkData()
    {
        Dictionary<Vector2Int, byte[]> result = new Dictionary<Vector2Int, byte[]>();
        string path = Application.persistentDataPath + "/save.json";

        if (!System.IO.File.Exists(path))
        {
            return result;
        }

        string json = System.IO.File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data.modifiedChunks != null)
        {
            foreach (var entry in data.modifiedChunks)
            {
                result[new Vector2Int(entry.chunkX, entry.chunkZ)] = entry.voxelData;
            }
        }

        return result;
    }
}