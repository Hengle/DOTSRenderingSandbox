using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities.Streaming;
#if UNITY_EDITOR
using UnityEditor;
#endif


#if UNITY_EDITOR
[CustomEditor(typeof(VegetationPlacer))]
public class VegetationPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        //if (GUILayout.Button("Place"))
        //{
        //    VegetationPlacer v = target as VegetationPlacer;
        //    v.SpawnVegetation();
        //}
    }
}
#endif

public enum VegetationSpawnerMethod
{
    GameObject,
    MeshCombine,
    DOTS,
}

public class VegetationPlacer : MonoBehaviour
{
    [Header("Params")]
    public VegetationSpawnerMethod Method;
    public GameObject VegetationPrefab;
    public int Quantity = 1000;
    public bool RemovePerInstanceCulling = false;

    [Header("References")]
    public BoxCollider Bounds;
    public Collider OnCollider;

    private EntityManager _entityManager;
    private Entity _prefabEntity;

    private Transform _spawnRoot;
    private Transform _tmpTransform;
    private Mesh _finalMesh;
    private Mesh _grassMesh;
    private Material _grassMat;
    private CombineInstance[] _combineInstances;

    private void Start()
    {
        SpawnVegetation();
    }

    public void SpawnVegetation()
    {
        if (Method == VegetationSpawnerMethod.DOTS)
        {
            _prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(VegetationPrefab, World.Active);
            _entityManager = World.Active.EntityManager;
            if (RemovePerInstanceCulling)
            {
                _entityManager.RemoveComponent<PerInstanceCullingTag>(_prefabEntity);
            }
        }
        else if (Method == VegetationSpawnerMethod.MeshCombine)
        {
            _finalMesh = new Mesh();
            _finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _tmpTransform = new GameObject("TMP").transform;
            _grassMesh = VegetationPrefab.GetComponentInChildren<MeshFilter>().sharedMesh;
            _grassMat = VegetationPrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            _combineInstances = new CombineInstance[Quantity];
        }
        else if (Method == VegetationSpawnerMethod.GameObject)
        {
            _spawnRoot = new GameObject(this.gameObject.name + "_Spawn").transform;
        }

        Transform thisTransform = this.transform;
        float xsize = Bounds.size.x * 0.5f;
        float ysize = Bounds.size.y * 0.5f;
        float zsize = Bounds.size.z * 0.5f;
        Vector3 highBoundsCenter = thisTransform.position + Bounds.center + (thisTransform.rotation * Vector3.up * ysize);

        // Spawn
        for (int i = 0; i < Quantity; i++)
        {
            Vector3 randOrigin = highBoundsCenter + (thisTransform.rotation * new Vector3(UnityEngine.Random.Range(-xsize, xsize), 0f, UnityEngine.Random.Range(-zsize, zsize)));

            Ray r = new Ray(randOrigin , - thisTransform.up);
            if(OnCollider.Raycast(r, out RaycastHit hit, ysize * 2f))
            {
                Vector3 upDir = hit.normal;
                Quaternion randomRot = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 30f), Vector3.right);
                randomRot = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up) * randomRot;
                randomRot = Quaternion.FromToRotation(Vector3.up, upDir) * randomRot;

                SpawnOne(i, hit.point, randomRot);
            }
        }

        // Mesh combine
        if (Method == VegetationSpawnerMethod.MeshCombine)
        {
            _finalMesh.CombineMeshes(_combineInstances);
            _finalMesh.Optimize();
            _finalMesh.OptimizeIndexBuffers();
            _finalMesh.OptimizeReorderVertexBuffer();
            GameObject grassesObject = new GameObject(this.gameObject.name + "_Spawn");
            MeshFilter mf = grassesObject.AddComponent<MeshFilter>();
            MeshRenderer mr = grassesObject.AddComponent<MeshRenderer>();
            mf.sharedMesh = _finalMesh;
            mr.sharedMaterial = _grassMat;
            mr.shadowCastingMode = VegetationPrefab.GetComponentInChildren<MeshRenderer>().shadowCastingMode;

            if (Application.isPlaying)
            {
                Destroy(_tmpTransform.gameObject);
            }
            else
            {
                DestroyImmediate(_tmpTransform.gameObject);
            }
        }
        else if (Method == VegetationSpawnerMethod.DOTS)
        {
            EntitySceneOptimization.Optimize(World.Active);
        }
    }

    public void SpawnOne(int index, Vector3 pos, Quaternion rot)
    {
        if (Method == VegetationSpawnerMethod.DOTS)
        {
            var instance = _entityManager.Instantiate(_prefabEntity);
            _entityManager.SetComponentData(instance, new Translation { Value = pos });
            _entityManager.SetComponentData(instance, new Rotation { Value = rot });
            //_entityManager.AddComponentData<Static>(instance, new Static());
        }
        else if (Method == VegetationSpawnerMethod.MeshCombine)
        {
            _tmpTransform.position = pos;
            _tmpTransform.rotation = rot;
            _combineInstances[index].subMeshIndex = 0;
            _combineInstances[index].mesh = _grassMesh;
            _combineInstances[index].transform = _tmpTransform.localToWorldMatrix;
        }
        else if (Method == VegetationSpawnerMethod.GameObject)
        {
            GameObject.Instantiate(VegetationPrefab, pos, rot, _spawnRoot);
        }
    }
}
