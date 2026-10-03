using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public int chunkSize = 16;
    public int chunkHeight = 16;
    public float noiseScale = 0.2f;     // size of the lumps (smaller = bigger lumps)
    public float noiseStrength = 2f;    // how tall the lumps are
    public float grit = 0.3f;           // small random roughness on top
    public Texture2D heightMap;
    public float maxHeight = 20f;
    public Vector2Int chunkCoord;
    public int totalGridSize;
    public bool isModified = false; // Flag to indicate if the chunk has been modified
    public float[] layerdepths = {4f, 10f, 18f, 20f};
    public float[] layerhardness = { 1f, 2f, 3f, 4f, 7f };
    public int maxNeighborsToRemove = 2;


    private byte[,,] voxels;
    private byte[,,] layers;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int>[] layerTriangles;
    private Mesh mesh;
    private const byte isoLevel = 128;   // the threshold for solid vs. air
    byte GetDensity(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x > chunkSize || y >= chunkHeight || z > chunkSize)
        {
            return 0;   // outside the grid is air
        }
        return voxels[x, y, z];
    }
    private static readonly Vector3Int[] cornerOffsets =
    {
    new Vector3Int(0, 0, 0), // 0
    new Vector3Int(1, 0, 0), // 1
    new Vector3Int(1, 0, 1), // 2
    new Vector3Int(0, 0, 1), // 3
    new Vector3Int(0, 1, 0), // 4
    new Vector3Int(1, 1, 0), // 5
    new Vector3Int(1, 1, 1), // 6
    new Vector3Int(0, 1, 1), // 7
       };
    private static readonly int[,] edgeCorners =
    {
    { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },   // edges 0-3: bottom square
    { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },   // edges 4-7: top square
    { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 },   // edges 8-11: vertical pillars
    };


    public void InitializeFromSave(byte[] voxelData)
    {
        mesh = new Mesh();

        FillLayers();

        SetVoxelBytes(voxelData);
    }

    public void Initialize()
    {
        voxels = new byte[chunkSize + 1, chunkHeight, chunkSize + 1];
        FillVoxels();

        FillLayers();

        mesh = new Mesh();

        RebuildMesh();
    }

    bool IsSolid(int x, int y, int z)
    {
        return GetDensity(x, y, z) >= isoLevel;
    }

    void FillVoxels()
    {
        for (int x = 0; x <= chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z <= chunkSize; z++)
                {
                    float worldX = chunkCoord.x * chunkSize + x;
                    float worldZ = chunkCoord.y * chunkSize + z;

                    float surfaceHeight = SurfaceAt(worldX, worldZ);

                    float inside = surfaceHeight - y;

                    // big smooth lumps
                    float noise = Mathf.PerlinNoise(x * noiseScale, z * noiseScale);
                    noise = noise * 2 - 1;
                    inside += noise * noiseStrength;

                    // small random grit
                    float grittyNoise = Mathf.PerlinNoise(worldX * 5f, worldZ * 5f) * 2 - 1;
                    inside += grittyNoise * grit;

                    float density = 128 + inside * 128 / maxHeight;
                    voxels[x, y, z] = (byte)Mathf.Clamp(density, 0, 255);
                }
            }
        }
    }

    void BuildMesh()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = -1; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    MarchCube(x, y, z);
                }
            }
        }
    }    

    void RebuildMesh()
    {
        vertices.Clear();
        for (int i = 0; i < layerTriangles.Length; i++)
        {
            layerTriangles[i].Clear();
        }
        mesh.Clear();

        BuildMesh();

        mesh.SetVertices(vertices);
        mesh.subMeshCount = layerTriangles.Length;
        for (int i = 0; i < layerTriangles.Length; i++)
        {
            mesh.SetTriangles(layerTriangles[i], i);
        }

        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = null;
        GetComponent<MeshCollider>().sharedMesh = mesh;

    }

    public int[] DigSphere(Vector3 center, float radius, float strength)
    {
        int[] voxelsDug = new int[layerhardness.Length];
        bool changed = false;

        int minX = Mathf.FloorToInt(center.x - radius);
        int maxX = Mathf.CeilToInt(center.x + radius);

        int minZ = Mathf.FloorToInt(center.z - radius);
        int maxZ = Mathf.CeilToInt(center.z + radius);

        int minY = Mathf.FloorToInt(center.y - radius);
        int maxY = Mathf.CeilToInt(center.y + radius);

        minX = Mathf.Clamp(minX, 0, chunkSize);
        maxX = Mathf.Clamp(maxX, 0, chunkSize);

        minZ = Mathf.Clamp(minZ, 0, chunkSize);
        maxZ = Mathf.Clamp(maxZ, 0, chunkSize);

        minY = Mathf.Clamp(minY, 0, chunkHeight - 1);
        maxY = Mathf.Clamp(maxY, 0, chunkHeight - 1);

        // 1. the dig itself
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    float distance = Vector3.Distance(new Vector3(x, y, z), center);
                    if (distance > radius) continue;   // outside the scoop, skip

                    float falloff = 1 - distance / radius;
                    int layer = layers[x, y, z];
                    float hardness = layerhardness[layer];
                    float amount = strength * falloff / hardness;

                    bool wasSolid = IsSolid(x, y, z);

                    float newDensity = voxels[x, y, z] - amount;
                    voxels[x, y, z] = (byte)Mathf.Clamp(newDensity, 0, 255);
                    changed = true;

                    // it counts as "dug" only if it just crossed from solid to air
                    if (wasSolid && !IsSolid(x, y, z))
                    {
                        voxelsDug[layer]++;
                    }
                }
            }
        }

        // 2. cleanup: remove thin leftover spikes
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    // skip the shared border voxels between chunks
                    if (x == 0 || x == chunkSize || z == 0 || z == chunkSize) continue;

                    if (IsSolid(x, y, z) && CountSolidNeighbors(x, y, z) <= maxNeighborsToRemove)
                    {
                        voxels[x, y, z] = 0;
                        changed = true;
                    }
                }
            }
        }

        // 3. rebuild the mesh once, then report what was dug
        if (changed)
        {
            isModified = true;
            RebuildMesh();
        }

        return voxelsDug;
    }

    int GetCaseIndex(int x, int y, int z)
    {
        int caseIndex = 0;

        for (int i = 0; i < 8; i++)
        {
            Vector3Int corner = new Vector3Int(x, y, z) + cornerOffsets[i];

            if (IsSolid(corner.x, corner.y, corner.z))
            {
                caseIndex += 1 << i;   // add this corner's switch value
            }
        }

        return caseIndex;
    }

    void MarchCube(int x, int y, int z)
    {
        int caseIndex = GetCaseIndex(x, y, z);
        Vector3Int cubePos = new Vector3Int(x, y, z);

        int layer = 0;
        for (int i = 0; i < 8; i++)
        {
            if ((caseIndex & (1 << i)) != 0)   // is corner i solid?
            {
                Vector3Int cornerPos = cubePos + cornerOffsets[i];
                layer = Mathf.Max(layer, layers[cornerPos.x, cornerPos.y, cornerPos.z]);


            }
        }

        // walk through the row, one triangle (3 edges) at a time
        for (int i = 0; MarchingTables.triTable[caseIndex, i] != -1; i += 3)
        {
            int start = vertices.Count;

            for (int j = 0; j < 3; j++)
            {
                int edge = MarchingTables.triTable[caseIndex, i + j];

                // the two corners at the ends of this edge
                int cornerA = edgeCorners[edge, 0];
                int cornerB = edgeCorners[edge, 1];

                Vector3Int posA = cubePos + cornerOffsets[cornerA];
                Vector3Int posB = cubePos + cornerOffsets[cornerB];

                float densityA = GetDensity(posA.x, posA.y, posA.z);
                float densityB = GetDensity(posB.x, posB.y, posB.z);

                float t = (isoLevel - densityA) / (densityB - densityA);

                vertices.Add(Vector3.Lerp(posA, posB, t));
            }
            layerTriangles[layer].Add(start + 0);
            layerTriangles[layer].Add(start + 1);
            layerTriangles[layer].Add(start + 2);

        }
    }
    public int CountSolidVoxels()
    {
        int count = 0;

        for (int x = 0; x <= chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z <= chunkSize; z++)
                {
                    if (IsSolid(x, y, z))
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    public byte[] GetVoxelBytes()
    {
        int sizeX = chunkSize + 1;
        int sizeY = chunkHeight;
        int sizeZ = chunkSize + 1;
        byte[] flat = new byte[sizeX * sizeY * sizeZ];

        int index = 0;

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    flat[index++] = voxels[x, y, z];
                }
            }
        }

        return flat;
    }

    public void SetVoxelBytes(byte[] flat)
    {
        int sizeX = chunkSize + 1;
        int sizeY = chunkHeight;
        int sizeZ = chunkSize + 1;
        voxels = new byte[sizeX, sizeY, sizeZ];

        int index = 0;

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    voxels[x, y, z] = flat[index++];
                }
            }
        }
        isModified = true;
        RebuildMesh();
    }

    private void Awake()
    {
         layerTriangles = new List<int>[layerdepths.Length + 1];

        for (int i = 0; i < layerTriangles.Length; i++)
        {
            layerTriangles[i] = new List<int>();
        }
    }

    float SurfaceAt(float worldx, float worldz)
    {
        float u = worldx / (float)totalGridSize;
        float v = worldz / (float)totalGridSize;
        Color pixel = heightMap.GetPixelBilinear(u, v);
        float brightness = pixel.grayscale;
        float surfaceHeight = brightness * maxHeight;
        return surfaceHeight;
    }

    void FillLayers()
    {
        layers = new byte[chunkSize + 1, chunkHeight, chunkSize + 1];

        for (int x = 0; x <= chunkSize; x++)
        {
            for (int z = 0; z <= chunkSize; z++)
            {
                float worldX = chunkCoord.x * chunkSize + x;
                float worldZ = chunkCoord.y * chunkSize + z;
                
                float surfaceHeight = SurfaceAt(worldX, worldZ);

                for (int y = 0; y < chunkHeight; y++)
                {
                    float depth = surfaceHeight - y;

                    int layer = 0;
                    for (int i = 0; i < layerdepths.Length; i++)
                    {
                        if (depth > layerdepths[i])
                        {
                            layer = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    layers[x, y, z] = (byte)layer;
                }
            }
        }
    }

    int CountSolidNeighbors(int x, int y, int z)
    {
        int count = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz) != 1) continue; // only check direct neighbors
                    int nx = x + dx;
                    int ny = y + dy;
                    int nz = z + dz;
                    if (nx >= 0 && nx <= chunkSize && ny >= 0 && ny < chunkHeight && nz >= 0 && nz <= chunkSize)
                    {
                        if (IsSolid(nx, ny, nz))
                        {
                            count++;
                        }
                    }
                }
            }
        }
        return count;
    }
}
