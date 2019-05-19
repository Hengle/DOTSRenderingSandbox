# DOTS Rendering Sandbox

![Alt Text](https://i.gyazo.com/3972f8dcfe4739a84db5cc252b70ab4b.gif)

What is this project:

This project contains a scene with a flat ground, and a spawner script that spawns any number of any given prefab on that ground using a selected method. The spawning methods right now are:
- GameObject: creates each object with GameObject.Instantiate
- DOTS: creates each object with EntityManager.Instantiate, after an ECS conversion of the original prefab
- MeshCombine: gradually builds up a single mesh representing all of the spawned objects combined into one, and then creates a gameObject with that big mesh

How to use:
- Open in Unity 2019.1
- Open the _Project/Scenes/Env scene
- Find the "GrassSpawner" object in the scene
- Set the Method, VegetationPrefab, and Quantity parameters
- Press play and it'll spawn the grass