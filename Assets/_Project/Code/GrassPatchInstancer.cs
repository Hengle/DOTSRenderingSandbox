using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;



public class GrassPatchInstancer : MonoBehaviour
{
    public enum Mode
    {
        GameObject,
        DOTS,
    }

    [Header("Debug")]
    [ReadOnly]
    public int FinalSpawnCount = 0;

    [Header("Params")]
    public Mode SpawnMode;
    public int InstanceResolutionPerPatch = 100;
    public int PatchResolution = 10;
    public float PatchSize = 10;

    [Header("References")]
    public GameObject Prefab;
    public BoxCollider Bounds;
    public Collider OnCollider;

    private Transform _transform;

    private void OnValidate()
    {
        FinalSpawnCount = (InstanceResolutionPerPatch * InstanceResolutionPerPatch) * (PatchResolution * PatchResolution);
    }

    void Start()
    {
        Spawn();
    }

    public void Spawn()
    {
        EntityManager _entityManager = World.Active.EntityManager;
        Mesh _grassMesh = Prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
        Material _grassMat = Prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;

        _transform = this.transform;
        float xHalfSize = Bounds.size.x * 0.5f;
        float yHalfSize = Bounds.size.y * 0.5f;
        float zHalfSize = Bounds.size.z * 0.5f;
        Vector3 highBoundsCenter = _transform.position + Bounds.center + (_transform.rotation * Vector3.up * yHalfSize);

        float totalSize = PatchResolution * PatchSize;
        Vector3 bottomCorner = Vector3.one * (totalSize * -0.5f);
        bottomCorner.y = 0f;
        float instanceSpacing = PatchSize / (float)InstanceResolutionPerPatch;

        // Generate all patches
        for (int patchX = 0; patchX < PatchResolution; patchX++)
        {
            for (int patchY = 0; patchY < PatchResolution; patchY++)
            {
                GeneratePatch(bottomCorner + new Vector3(patchX * PatchSize, 0f, patchY * PatchSize), highBoundsCenter, instanceSpacing, yHalfSize * 2f, _grassMesh, _grassMat, _entityManager);
            }
        }
    }

    public void GeneratePatch(Vector3 start, Vector3 highBoundsCenter, float spacing, float rayDist, Mesh grassMesh, Material grassMat, EntityManager entityManager)
    {
        Mesh _finalMesh = new Mesh();
        _finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        CombineInstance[] _combineInstances = new CombineInstance[InstanceResolutionPerPatch * InstanceResolutionPerPatch];

        // Spawn individual grasses
        int counter = 0;
        for (int x = 0; x < InstanceResolutionPerPatch; x++)
        {
            for (int y = 0; y < InstanceResolutionPerPatch; y++)
            {
                Vector3 rayOrigin = start + new Vector3(x * spacing, 0f, y * spacing);
                rayOrigin = highBoundsCenter + (_transform.rotation * rayOrigin);

                Ray r = new Ray(rayOrigin, -_transform.up);
                if (OnCollider.Raycast(r, out RaycastHit hit, rayDist))
                {
                    Vector3 upDir = hit.normal;
                    Quaternion randomRot = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 30f), Vector3.right);
                    randomRot = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up) * randomRot;
                    randomRot = Quaternion.FromToRotation(Vector3.up, upDir) * randomRot;

                    SpawnInstance(counter, hit.point, randomRot, grassMesh, _combineInstances);
                    counter++;
                }
                else
                {
                    SpawnInstance(counter, rayOrigin + (-_transform.up * rayDist * 0.5f), Quaternion.identity, grassMesh, _combineInstances);
                }
            }
        }

        // Create the patch object
        _finalMesh.CombineMeshes(_combineInstances);
        _finalMesh.Optimize();
        _finalMesh.OptimizeIndexBuffers();
        _finalMesh.OptimizeReorderVertexBuffer();
        GameObject patchObject = new GameObject();
        MeshFilter mf = patchObject.AddComponent<MeshFilter>();
        MeshRenderer mr = patchObject.AddComponent<MeshRenderer>();
        mf.sharedMesh = _finalMesh;
        mr.sharedMaterial = grassMat;
        mr.shadowCastingMode = Prefab.GetComponentInChildren<MeshRenderer>().shadowCastingMode;

        // Convert to DOTS
        if (SpawnMode == Mode.DOTS)
        {
            Entity _prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(patchObject, World.Active);
            Destroy(patchObject);
        }
    }

    public void SpawnInstance(int index, Vector3 pos, Quaternion rot, Mesh grassMesh, CombineInstance[] combineInstances)
    {
        combineInstances[index].subMeshIndex = 0;
        combineInstances[index].mesh = grassMesh;
        combineInstances[index].transform = Matrix4x4.TRS(pos, rot, Vector3.one);
    }
}
