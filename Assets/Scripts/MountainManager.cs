using System.Collections.Generic;
using UnityEngine;

public class MountainManager : MonoBehaviour
{
    // start Serialized fields
    [Header("Prefabs")]
    public Chunk chunkPrefab;

    [Header("Grid")]
    public int gridWidth = 4;
    public int gridDepth = 4;

    [Header("Terrain")]
    public Texture2D heightMap;
    // end Serialized fields

    // start Runtime state
    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    // end Runtime state

    // start Start
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
    // end Start

    // start DigAt
    public int[] DigAt(Vector3 worldPosition, float radius, float strength)
    {
        int[] totalDug = new int[chunkPrefab.layerhardness.Length];
        int chunkSize = chunkPrefab.chunkSize;

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
    // end DigAt

    // start BuildChunkSaveData
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
    // end BuildChunkSaveData

    // start LoadSavedChunkData
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
    // end LoadSavedChunkData

    // start Center
    public Vector3 Center
    {
        get
        {
            float x = gridWidth / 2f * chunkPrefab.chunkSize;
            float z = gridDepth / 2f * chunkPrefab.chunkSize;
            return new Vector3(x, 0f, z);
        }
    }
    // end Chunk Center

    // start OuterRadius
    public float OuterRadius
    {
        get 
        {
            float w = gridWidth * chunkPrefab.chunkSize;
            float d = gridDepth * chunkPrefab.chunkSize;
            return Mathf.Sqrt(w * w + d * d) / 2f + 5f;
        }
    }
    // end OuterRadius

    // start TryFindSolid
    public bool TryFindSolid(Vector3 center, float radius, float height, float minY, out Vector3 point)
    {
        int chunkSize = chunkPrefab.chunkSize;
        float bestDistance = float.MaxValue;
        point = Vector3.zero;
        bool found = false;

        int minX = Mathf.FloorToInt(center.x - radius);
        int maxX = Mathf.CeilToInt(center.x + radius);
        int minZ = Mathf.FloorToInt(center.z - radius);
        int maxZ = Mathf.CeilToInt(center.z + radius);

        for (int wx = minX; wx <= maxX; wx++)
        {
            for (int wz = minZ; wz <= maxZ; wz++)
            {
                float flatDistance = Vector2.Distance(new Vector2(wx, wz), new Vector2(center.x, center.z));
                if (flatDistance > radius)
                {
                    continue;
                }
                int yStart = Mathf.CeilToInt(minY);
                int yEnd = Mathf.FloorToInt(center.y + height);

                for (int y = yStart; y <= yEnd; y++)
                {
                    int gx = Mathf.FloorToInt(wx / (float)chunkSize);
                    int gz = Mathf.FloorToInt(wz / (float)chunkSize);

                    if (chunks.TryGetValue(new Vector2Int(gx, gz), out Chunk chunk))
                    {
                        int localX = wx - gx * chunkSize;
                        int localZ = wz - gz * chunkSize;

                        if (chunk.IsSolid(localX, y, localZ))
                        {
                            Vector3 cell = new Vector3(wx, y, wz);
                            float distance = Vector3.Distance(cell, center);

                            if (distance <= bestDistance)
                            {
                                bestDistance = distance;
                                point = cell;
                                found = true;
                            }
                        }
                    }
                }
            }
        }

        return found;
    }
    //end TryFindSolid
}