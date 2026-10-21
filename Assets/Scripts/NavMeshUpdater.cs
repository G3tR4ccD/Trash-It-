using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshUpdater : MonoBehaviour
{
    public NavMeshSurface surface;
    public float interval = 5f;

    IEnumerator Start()
    {
        yield return null;               // wait a frame so the chunks exist
        surface.BuildNavMesh();
        while (true)
        {
            yield return new WaitForSeconds(interval);
            surface.UpdateNavMesh(surface.navMeshData);
        }
    }
}