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

    void Start()
    {

        voxels = new byte[chunkSize, chunkSize, chunkSize];
        FillVoxels();

        mesh = new Mesh();

        RebuildMesh();

    }
    void AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int start = vertices.Count;   // how many corners exist BEFORE this face

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        // triangle 1: a, b, c
        triangles.Add(start + 0);
        triangles.Add(start + 1);
        triangles.Add(start + 2);

        // triangle 2: a, c, d
        triangles.Add(start + 0);
        triangles.Add(start + 2);
        triangles.Add(start + 3);

    }
    void AddCube(int x, int y, int z)
    {
        Vector3 position = new Vector3(x, y, z);

        if (!IsSolid(x, y, z - 1))   // front (-Z)
            AddFace(position + new Vector3(0, 0, 0), position + new Vector3(0, 1, 0), position + new Vector3(1, 1, 0), position + new Vector3(1, 0, 0));

        if (!IsSolid(x, y, z + 1))           // back (+Z)
            AddFace(position + new Vector3(0, 0, 1), position + new Vector3(1, 0, 1), position + new Vector3(1, 1, 1), position + new Vector3(0, 1, 1));

        if (!IsSolid(x, y - 1, z))           // bottom (-Y)
            AddFace(position + new Vector3(0, 0, 0), position + new Vector3(1, 0, 0), position + new Vector3(1, 0, 1), position + new Vector3(0, 0, 1));

        if (!IsSolid(x, y + 1, z))           // top (+Y)
            AddFace(position + new Vector3(0, 1, 0), position + new Vector3(0, 1, 1), position + new Vector3(1, 1, 1), position + new Vector3(1, 1, 0));

        if (!IsSolid(x - 1, y, z))           // left (-X)
            AddFace(position + new Vector3(0, 0, 0), position + new Vector3(0, 0, 1), position + new Vector3(0, 1, 1), position + new Vector3(0, 1, 0));

        if (!IsSolid(x + 1, y, z))           // right (+X)
            AddFace(position + new Vector3(1, 0, 0), position + new Vector3(1, 1, 0), position + new Vector3(1, 1, 1), position + new Vector3(1, 0, 1));
    }
    bool IsSolid(int x, int y, int z)
    {
        // outside the grid? then it counts as air
        if (x < 0 || y < 0 || z < 0 || x >= chunkSize || y >= chunkSize || z >= chunkSize)
        {
            return false;
        }

        // inside the grid: is this voxel solid?
        return voxels[x, y, z] == 1;
    }

    void FillVoxels()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (y < 8)
                    {
                        voxels[x, y, z] = 1;   // solid
                    }
                    else
                    {
                        voxels[x, y, z] = 0;   // air
                    }
                }
            }
        }
    }

    void BuildMesh()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (voxels[x, y, z] == 1)
                    {
                        AddCube(x, y, z);
                    }
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

    public int DigSphere(Vector3 center, float radius)
    {
        int voxelsDug = 0;
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Vector3 voxelPosition = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                    if (IsSolid(x, y, z) && Vector3.Distance(voxelPosition, center) <= radius)
                    {
                        voxels[x, y, z] = 0; // set to air
                        voxelsDug++;
                    }
                }
            }
        }
        if (voxelsDug > 0)
        {
            RebuildMesh();
        }
        return voxelsDug;
    }


}
