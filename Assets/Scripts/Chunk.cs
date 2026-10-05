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
    public int maxIslandSize = 40;       // max voxel count for a disconnected chunk to be auto-removed
    public int islandSearchPadding = 4;  // how far beyond the dig box to look for floating debris

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

    bool FloodFillIsland(int startX, int startY, int startZ, int padX0, int padX1, int padY0, int padY1, int padZ0, int padZ1, bool[,,] visited, List<Vector3Int> island)
    {
        bool touchesBoundary = false;
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        stack.Push(new Vector3Int(startX, startY, startZ));
        visited[startX - padX0, startY - padY0, startZ - padZ0] = true;

        Vector3Int[] directions = {
        new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
        new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
        new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
    };

        while (stack.Count > 0)
        {
            Vector3Int current = stack.Pop();
            island.Add(current);

            if (current.x == padX0 || current.x == padX1 || current.y == padY0 || current.y == padY1 || current.z == padZ0 || current.z == padZ1)
            {
                touchesBoundary = true;
            }

            foreach (var dir in directions)
            {
                int nx = current.x + dir.x;
                int ny = current.y + dir.y;
                int nz = current.z + dir.z;

                if (nx < padX0 || nx > padX1 || ny < padY0 || ny > padY1 || nz < padZ0 || nz > padZ1) continue;
                if (visited[nx - padX0, ny - padY0, nz - padZ0]) continue;
                if (!IsSolid(nx, ny, nz)) continue;

                visited[nx - padX0, ny - padY0, nz - padZ0] = true;
                stack.Push(new Vector3Int(nx, ny, nz));
            }

            if (island.Count > maxIslandSize * 4)
            {
                touchesBoundary = true; // bail out, this is too big to be simple debris
                break;
            }
        }

        return touchesBoundary;
    }
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

        // 2. cleanup: remove small disconnected floating chunks
        int padX0 = Mathf.Clamp(minX - islandSearchPadding, 0, chunkSize);
        int padX1 = Mathf.Clamp(maxX + islandSearchPadding, 0, chunkSize);
        int padY0 = Mathf.Clamp(minY - islandSearchPadding, 0, chunkHeight - 1);
        int padY1 = Mathf.Clamp(maxY + islandSearchPadding, 0, chunkHeight - 1);
        int padZ0 = Mathf.Clamp(minZ - islandSearchPadding, 0, chunkSize);
        int padZ1 = Mathf.Clamp(maxZ + islandSearchPadding, 0, chunkSize);

        bool[,,] visited = new bool[padX1 - padX0 + 1, padY1 - padY0 + 1, padZ1 - padZ0 + 1];

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (!IsSolid(x, y, z)) continue;
                    if (visited[x - padX0, y - padY0, z - padZ0]) continue;

                    List<Vector3Int> island = new List<Vector3Int>();
                    bool touchesBoundary = FloodFillIsland(x, y, z, padX0, padX1, padY0, padY1, padZ0, padZ1, visited, island);

                    if (!touchesBoundary && island.Count <= maxIslandSize)
                    {
                        foreach (var voxel in island)
                        {
                            voxels[voxel.x, voxel.y, voxel.z] = 0;
                        }
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
}
