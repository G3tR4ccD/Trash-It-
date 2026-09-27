using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public int chunkSize = 16;
    private byte[,,] voxels;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private Mesh mesh;
    private const byte isoLevel = 128;   // the threshold for solid vs. air

    byte GetDensity(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= chunkSize || y >= chunkSize || z >= chunkSize)
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

    public float noiseScale = 0.2f;     // size of the lumps (smaller = bigger lumps)
    public float noiseStrength = 2f;    // how tall the lumps are
    public float grit = 0.3f;           // small random roughness on top


    void Start()
    {

        voxels = new byte[chunkSize, chunkSize, chunkSize];
        FillVoxels();

        mesh = new Mesh();

        RebuildMesh();

    }

    bool IsSolid(int x, int y, int z)
    {
        return GetDensity(x, y, z) >= isoLevel;
    }

    void FillVoxels()
    {
        Vector3 center = new Vector3(8, 0, 8);
        float radius = 7f;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float distance = Vector3.Distance(new Vector3(x, y, z), center);
                    float inside = radius - distance;

                    // big smooth lumps
                    float noise = Mathf.PerlinNoise(x * noiseScale, z * noiseScale);   // gives 0 to 1
                    noise = noise * 2 - 1;                                  // now -1 to 1
                    inside += noise * noiseStrength;

                    // small random grit
                    inside += Random.Range(-grit, grit);

                    float density = 128 + inside * 128 / radius;
                    voxels[x, y, z] = (byte)Mathf.Clamp(density, 0, 255);
                }
            }
        }
    }

    void BuildMesh()
    {
        for (int x = -1; x < chunkSize; x++)
        {
            for (int y = -1; y < chunkSize; y++)
            {
                for (int z = -1; z < chunkSize; z++)
                {
                    MarchCube(x, y, z);
                }
            }
        }
    }    

    void RebuildMesh()
    {
        vertices.Clear();
        triangles.Clear();
        mesh.Clear();

        BuildMesh();

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = null;
        GetComponent<MeshCollider>().sharedMesh = mesh;

    }



    public int DigSphere(Vector3 center, float radius, float strength)
    {
        int voxelsDug = 0;
        bool changed = false;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float distance = Vector3.Distance(new Vector3(x, y, z), center);
                    if (distance > radius) continue;   // outside the scoop, skip

                    float falloff = 1 - distance / radius;
                    float amount = strength * falloff;

                    bool wasSolid = IsSolid(x, y, z);

                    float newDensity = voxels[x, y, z] - amount;
                    voxels[x, y, z] = (byte)Mathf.Clamp(newDensity, 0, 255);
                    changed = true;

                    // it counts as "dug" only if it just crossed from solid to air
                    if (wasSolid && !IsSolid(x, y, z))
                    {
                        voxelsDug++;
                    }
                }
            }
        }

        if (changed)
        {
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

                float densityA = GetDensity(posA.x, posA.y, posA.z);   // won't compile yet, see below
                float densityB = GetDensity(posB.x, posB.y, posB.z);

                float t = (isoLevel - densityA) / (densityB - densityA);

                vertices.Add(Vector3.Lerp(posA, posB, t));
            }

            triangles.Add(start + 0);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }
    }

}
