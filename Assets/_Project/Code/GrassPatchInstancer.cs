using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Streaming;
using Unity.Rendering;
using Unity.Scenes;
using Unity.Transforms;
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
    public GameObject Prefab;
    public Mode SpawnMode;
    public int InstanceResolutionPerPatch = 100;
    public int PatchResolution = 10;
    public float PatchSize = 10;
    public SubScene GrassSubScene;

    private Transform _transform;
    private const float MaxSpawnHeight = 10000;

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
        FrozenRenderSceneTag frozenTag = new FrozenRenderSceneTag()
        {
            SceneGUID = GrassSubScene.SceneGUID,
            SectionIndex = 0,
            HasStreamedLOD = 0,
        };

        EntityManager entityManager = World.Active.EntityManager;
        Mesh grassMesh = Prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
        Material grassMat = Prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;

        _transform = this.transform;
        Vector3 highBoundsCenter = _transform.position + (_transform.rotation * Vector3.up * MaxSpawnHeight);

        float totalSize = PatchResolution * PatchSize;
        Vector3 bottomCorner = Vector3.one * (totalSize * -0.5f);
        bottomCorner.y = 0f;
        float instanceSpacing = PatchSize / (float)InstanceResolutionPerPatch;

        // Generate all patches
        for (int patchX = 0; patchX < PatchResolution; patchX++)
        {
            for (int patchY = 0; patchY < PatchResolution; patchY++)
            {
                GeneratePatch(bottomCorner + new Vector3(patchX * PatchSize, 0f, patchY * PatchSize), highBoundsCenter, instanceSpacing, MaxSpawnHeight * 2f, grassMesh, grassMat, entityManager, frozenTag);
            }
        }

        if (SpawnMode == Mode.DOTS)
        {
            //EntitySceneOptimization.Optimize(World.Active);
        }
    }

    public void GeneratePatch(Vector3 start, Vector3 highBoundsCenter, float spacing, float rayDist, Mesh grassMesh, Material grassMat, EntityManager entityManager, FrozenRenderSceneTag frozenTag)
    {
        Mesh finalMesh = new Mesh();
        finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
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
                if (Physics.Raycast(r, out RaycastHit hit, rayDist))
                {
                    Vector3 upDir = hit.normal;
                    Quaternion randomRot = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 30f), Vector3.right);
                    randomRot = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 180), Vector3.up) * randomRot;
                    randomRot = Quaternion.FromToRotation(Vector3.up, upDir) * randomRot;

                    SpawnInstance(counter, hit.point, randomRot, Prefab.transform.localScale, grassMesh, _combineInstances);
                    counter++;
                }
                else
                {
                    SpawnInstance(counter, rayOrigin + (-_transform.up * rayDist * 0.5f), Quaternion.identity, Prefab.transform.localScale, grassMesh, _combineInstances);
                    counter++;
                }
            }
        }

        // Create the patch object
        finalMesh.CombineMeshes(_combineInstances);
        finalMesh.RecalculateBounds();
        finalMesh.Optimize();
        finalMesh.OptimizeIndexBuffers();
        finalMesh.OptimizeReorderVertexBuffer();
        GameObject patchObject = new GameObject();
        MeshFilter mf = patchObject.AddComponent<MeshFilter>();
        MeshRenderer mr = patchObject.AddComponent<MeshRenderer>();
        mf.sharedMesh = finalMesh;
        mr.sharedMaterial = grassMat;
        mr.shadowCastingMode = Prefab.GetComponentInChildren<MeshRenderer>().shadowCastingMode;

        // Convert to DOTS
        if (SpawnMode == Mode.DOTS)
        {
            Entity prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(patchObject, World.Active);
            entityManager.AddComponentData<Static>(prefabEntity, new Static());
            entityManager.AddSharedComponentData<FrozenRenderSceneTag>(prefabEntity, frozenTag);

            Destroy(patchObject);
        }
        else
        {
            //patchObject.isStatic = true;
        }
    }

    public void SpawnInstance(int index, Vector3 pos, Quaternion rot, Vector3 scale, Mesh grassMesh, CombineInstance[] combineInstances)
    {
        combineInstances[index].subMeshIndex = 0;
        combineInstances[index].mesh = grassMesh;
        combineInstances[index].transform = Matrix4x4.TRS(pos, rot, scale);
    }
}
