#if UNITY_EDITOR
using System.Collections.Generic;
using SeekingForJade.Economy;
using SeekingForJade.Environment;
using SeekingForJade.Jade;
using SeekingForJade.Player;
using SeekingForJade.Slicing;
using SeekingForJade.Tools;
using SeekingForJade.Workstations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeekingForJade.Editor
{
    public static class SceneSetupHelper
    {
        [MenuItem("SeekingForJade/Setup Prototype Scene")]
        public static void SetupScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();
            if (roots.Length > 0)
            {
                Undo.RegisterFullObjectHierarchyUndo(roots[0], "Setup Jade Prototype");
            }

            // 1. Ensure all stylized materials exist
            MaterialMaterials mats = EnsureMaterials();

            // 2. Low-Poly Environment, Terrain, Paths & Workshop Shelter
            SetupLowPolyEnvironment(mats);

            // 3. Configure Player Prefab with Low-Poly First-Person Hands & Third-Person Avatar
            SetupPlayerPrefab(mats);

            // 4. Setup Cutting Workbench in Scene
            SetupCuttingStation(mats);

            // 5. Setup Mining Quarry & Trader Stall
            SetupEconomyAndQuarry(mats);

            // 6. Spawn Diverse Test Rocks (including Tape-Wrapped Boulder!)
            SpawnTestRocks();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green><b>[SeekingForJade]</b></color> Low-poly scene setup complete! Faceted terrain, pine trees, workshop shelter, first-person hands, and tape-wrapped mystery rock ready.");
        }

        public class MaterialMaterials
        {
            public Material grass;
            public Material dirt;
            public Material wood;
            public Material foliage;
            public Material character;
            public Material skin;
            public Material metal;
            public Material roof;
            public Material lantern;
            public Material jade;
            public Material merchantSilk;
            public Material gold;
            public Material darkHair;
        }

        private static MaterialMaterials EnsureMaterials()
        {
            MaterialMaterials m = new MaterialMaterials();

            m.grass = GetOrCreateMaterial("Assets/Materials/M_Stylized_Grass.mat", new Color(0.32f, 0.54f, 0.28f), 0.15f);
            m.dirt = GetOrCreateMaterial("Assets/Materials/M_Stylized_DirtPath.mat", new Color(0.55f, 0.44f, 0.32f), 0.10f);
            m.wood = GetOrCreateMaterial("Assets/Materials/M_Stylized_Wood.mat", new Color(0.42f, 0.28f, 0.18f), 0.20f);
            m.foliage = GetOrCreateMaterial("Assets/Materials/M_Stylized_Foliage.mat", new Color(0.18f, 0.38f, 0.22f), 0.10f);
            m.character = GetOrCreateMaterial("Assets/Materials/M_Stylized_Character.mat", new Color(0.22f, 0.35f, 0.48f), 0.25f);
            m.skin = GetOrCreateMaterial("Assets/Materials/M_Stylized_Skin.mat", new Color(0.88f, 0.70f, 0.56f), 0.25f);
            m.metal = GetOrCreateMaterial("Assets/Materials/M_Stylized_Metal.mat", new Color(0.28f, 0.30f, 0.32f), 0.65f, 0.85f);
            m.roof = GetOrCreateMaterial("Assets/Materials/M_Stylized_Roof.mat", new Color(0.52f, 0.26f, 0.16f), 0.20f);
            m.lantern = GetOrCreateMaterial("Assets/Materials/M_Stylized_Lantern.mat", new Color(1.0f, 0.85f, 0.50f), 0.10f);
            m.jade = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Jade_Internal.mat");
            m.merchantSilk = GetOrCreateMaterial("Assets/Materials/M_Stylized_MerchantSilk.mat", new Color(0.12f, 0.28f, 0.22f), 0.45f);
            m.gold = GetOrCreateMaterial("Assets/Materials/M_Stylized_Gold.mat", new Color(0.86f, 0.72f, 0.28f), 0.75f, 0.85f);
            m.darkHair = GetOrCreateMaterial("Assets/Materials/M_Stylized_DarkHair.mat", new Color(0.12f, 0.12f, 0.12f), 0.15f);

            return m;
        }

        private static Material GetOrCreateMaterial(string path, Color baseColor, float smoothness, float metallic = 0f)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                mat.SetColor("_BaseColor", baseColor);
                mat.SetFloat("_Smoothness", smoothness);
                mat.SetFloat("_Metallic", metallic);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void SetupLowPolyEnvironment(MaterialMaterials mats)
        {
            // Remove old flat plane if it exists
            GameObject oldPlane = GameObject.Find("Plane");
            if (oldPlane != null)
            {
                Object.DestroyImmediate(oldPlane);
            }

            // Remove old starter Cube if present
            GameObject cube = GameObject.Find("Cube");
            if (cube != null)
            {
                Object.DestroyImmediate(cube);
            }

            // 1. Faceted Low-Poly Terrain
            GameObject terrainObj = GameObject.Find("FacetedTerrain");
            if (terrainObj != null) Object.DestroyImmediate(terrainObj);

            terrainObj = new GameObject("FacetedTerrain");
            terrainObj.transform.position = Vector3.zero;
            MeshFilter tf = terrainObj.AddComponent<MeshFilter>();
            MeshRenderer tr = terrainObj.AddComponent<MeshRenderer>();
            MeshCollider tc = terrainObj.AddComponent<MeshCollider>();

            Mesh terrainMesh = LowPolyMeshGenerator.GenerateFacetedTerrain(36, 1.4f);
            tf.sharedMesh = terrainMesh;
            tr.sharedMaterial = mats.grass;
            tc.sharedMesh = terrainMesh;

            // 2. Stylized Dirt Paths
            GameObject pathObj = GameObject.Find("DirtPaths");
            if (pathObj != null) Object.DestroyImmediate(pathObj);

            pathObj = new GameObject("DirtPaths");
            pathObj.transform.position = Vector3.zero;

            // Path 1: Workbench to Quarry
            CreatePathSegment(pathObj.transform, new Vector3[]
            {
                new Vector3(0f, 0.01f, 3.2f),
                new Vector3(-1.8f, 0.01f, 3.0f),
                new Vector3(-3.5f, 0.01f, 2.5f)
            }, mats.dirt, 1.4f);

            // Path 2: Workbench to Trader
            CreatePathSegment(pathObj.transform, new Vector3[]
            {
                new Vector3(0f, 0.01f, 3.2f),
                new Vector3(1.8f, 0.01f, 3.0f),
                new Vector3(3.5f, 0.01f, 2.5f)
            }, mats.dirt, 1.4f);

            // Path 3: Spawn clearing path
            CreatePathSegment(pathObj.transform, new Vector3[]
            {
                new Vector3(0f, 0.01f, -2.5f),
                new Vector3(0f, 0.01f, 0.5f),
                new Vector3(0f, 0.01f, 3.2f)
            }, mats.dirt, 1.6f);

            // 3. Perimeter Trees & Boulders
            GameObject treeGroup = GameObject.Find("Environment_Vegetation");
            if (treeGroup != null) Object.DestroyImmediate(treeGroup);

            treeGroup = new GameObject("Environment_Vegetation");

            // Seeded tree ring around the clearing using SimpleNaturePack
            string[] treePrefabPaths = new string[]
            {
                "Assets/SimpleNaturePack/Prefabs/Tree_01.prefab",
                "Assets/SimpleNaturePack/Prefabs/Tree_02.prefab",
                "Assets/SimpleNaturePack/Prefabs/Tree_03.prefab",
                "Assets/SimpleNaturePack/Prefabs/Tree_04.prefab",
                "Assets/SimpleNaturePack/Prefabs/Tree_05.prefab"
            };

            var rng = new System.Random(42);
            int treeCount = 34;
            for (int i = 0; i < treeCount; i++)
            {
                float angle = i * (Mathf.PI * 2f / treeCount) + ((float)rng.NextDouble() - 0.5f) * 0.25f;
                float radius = Mathf.Lerp(9.5f, 17.5f, (float)rng.NextDouble());

                float x = Mathf.Cos(angle) * radius;
                float z = 3.0f + Mathf.Sin(angle) * radius;

                // Sample terrain height roughly
                float dist = Mathf.Sqrt(x * x + (z - 3f) * (z - 3f));
                float y = dist > 7.5f ? Mathf.Pow((dist - 7.5f) / 12f, 1.8f) * 3.5f : 0f;

                string tPath = treePrefabPaths[i % treePrefabPaths.Length];
                GameObject treeObj = SpawnPrefab(tPath, treeGroup.transform, new Vector3(x, y, z), Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * Mathf.Lerp(0.85f, 1.35f, (float)rng.NextDouble()));
                if (treeObj == null)
                {
                    treeObj = new GameObject($"Tree_{i}");
                    treeObj.transform.SetParent(treeGroup.transform);
                    treeObj.transform.position = new Vector3(x, y, z);
                    treeObj.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

                    MeshFilter mf = treeObj.AddComponent<MeshFilter>();
                    MeshRenderer mr = treeObj.AddComponent<MeshRenderer>();
                    mf.sharedMesh = LowPolyMeshGenerator.GeneratePineTree(100 + i, 5.0f);
                    mr.sharedMaterials = new Material[] { mats.wood, mats.foliage };
                }

                // Add under-tree foliage (bushes, flowers, mushrooms)
                if (rng.NextDouble() > 0.35)
                {
                    string bushPath = $"Assets/SimpleNaturePack/Prefabs/Bush_0{(i % 3) + 1}.prefab";
                    Vector3 bushPos = new Vector3(x + ((float)rng.NextDouble() - 0.5f) * 1.5f, y, z + ((float)rng.NextDouble() - 0.5f) * 1.5f);
                    SpawnPrefab(bushPath, treeGroup.transform, bushPos, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * Mathf.Lerp(0.7f, 1.1f, (float)rng.NextDouble()));
                }

                if (rng.NextDouble() > 0.55)
                {
                    string flowerPath = (i % 2 == 0) ? "Assets/SimpleNaturePack/Prefabs/Flowers_01.prefab" : "Assets/SimpleNaturePack/Prefabs/Flowers_02.prefab";
                    Vector3 flowerPos = new Vector3(x + ((float)rng.NextDouble() - 0.5f) * 1.8f, y, z + ((float)rng.NextDouble() - 0.5f) * 1.8f);
                    SpawnPrefab(flowerPath, treeGroup.transform, flowerPos, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * 0.85f);
                }

                if (rng.NextDouble() > 0.70)
                {
                    string shroomPath = (i % 2 == 0) ? "Assets/SimpleNaturePack/Prefabs/Mushroom_01.prefab" : "Assets/SimpleNaturePack/Prefabs/Mushroom_02.prefab";
                    Vector3 shroomPos = new Vector3(x + ((float)rng.NextDouble() - 0.5f) * 1.2f, y, z + ((float)rng.NextDouble() - 0.5f) * 1.2f);
                    SpawnPrefab(shroomPath, treeGroup.transform, shroomPos, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * 0.75f);
                }
            }

            // Scatter tree stumps and fallen branches in the glade
            SpawnPrefab("Assets/SimpleNaturePack/Prefabs/Stump_01.prefab", treeGroup.transform, new Vector3(-5.2f, 0.1f, 1.2f), Quaternion.Euler(0f, 45f, 0f), Vector3.one);
            SpawnPrefab("Assets/SimpleNaturePack/Prefabs/Branch_01.prefab", treeGroup.transform, new Vector3(-4.8f, 0.05f, 0.9f), Quaternion.Euler(0f, 110f, 0f), Vector3.one);
            SpawnPrefab("Assets/SimpleNaturePack/Prefabs/Stump_01.prefab", treeGroup.transform, new Vector3(5.5f, 0.1f, 5.0f), Quaternion.Euler(0f, 85f, 0f), Vector3.one);

            // Perimeter faceted boulders from BrokenVector LowPolyRockPack
            for (int i = 0; i < 12; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = Mathf.Lerp(7.0f, 12.0f, (float)rng.NextDouble());
                Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0.1f, 3.0f + Mathf.Sin(angle) * dist);

                int rockType = (i % 6) + 1;
                int variation = ((i * 2) % 4) + 1;
                string rockPath = $"Assets/BrokenVector/LowPolyRockPack/Prefabs/Rock Type{rockType} 0{variation}.prefab";

                GameObject rObj = SpawnPrefab(rockPath, treeGroup.transform, pos, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), Vector3.one * Mathf.Lerp(0.8f, 1.6f, (float)rng.NextDouble()));
                if (rObj == null)
                {
                    GameObject boulder = new GameObject($"DecoBoulder_{i}");
                    boulder.transform.SetParent(treeGroup.transform);
                    boulder.transform.position = pos;
                    boulder.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                    MeshFilter mf = boulder.AddComponent<MeshFilter>();
                    MeshRenderer mr = boulder.AddComponent<MeshRenderer>();
                    mf.sharedMesh = LowPolyMeshGenerator.GenerateLowPolyBoulder(300 + i, 0.8f);
                    mr.sharedMaterial = mats.dirt;
                }
            }

            // 4. Workshop Lean-To Shelter over Cutting Station
            SetupWorkshopShelter(mats);
        }

        private static void CreatePathSegment(Transform parent, Vector3[] waypoints, Material dirtMat, float width)
        {
            GameObject segment = new GameObject("PathSegment");
            segment.transform.SetParent(parent);
            segment.transform.position = Vector3.zero;

            MeshFilter mf = segment.AddComponent<MeshFilter>();
            MeshRenderer mr = segment.AddComponent<MeshRenderer>();

            mf.sharedMesh = LowPolyMeshGenerator.GeneratePathRibbon(waypoints, width);
            mr.sharedMaterial = dirtMat;
        }

        private static void SetupWorkshopShelter(MaterialMaterials mats)
        {
            GameObject existingShelter = GameObject.Find("WorkshopShelter");
            if (existingShelter != null) Object.DestroyImmediate(existingShelter);

            GameObject shelter = new GameObject("WorkshopShelter");
            shelter.transform.position = new Vector3(0f, 0f, 3.5f);

            // 4 Corner Timber Posts
            Vector3[] postPositions = new Vector3[]
            {
                new Vector3(-1.6f, 1.4f, -1.2f),
                new Vector3( 1.6f, 1.4f, -1.2f),
                new Vector3(-1.6f, 1.25f, 1.2f),
                new Vector3( 1.6f, 1.25f, 1.2f)
            };

            for (int i = 0; i < postPositions.Length; i++)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"Post_{i}";
                post.transform.SetParent(shelter.transform);
                post.transform.localPosition = postPositions[i];
                post.transform.localScale = new Vector3(0.18f, 2.8f, 0.18f);
                post.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;
            }

            // Header Beams (connecting posts)
            CreateBeam(shelter.transform, new Vector3(0f, 2.75f, -1.2f), new Vector3(3.4f, 0.16f, 0.16f), mats.wood);
            CreateBeam(shelter.transform, new Vector3(0f, 2.45f,  1.2f), new Vector3(3.4f, 0.16f, 0.16f), mats.wood);
            CreateBeam(shelter.transform, new Vector3(-1.6f, 2.6f, 0f), new Vector3(0.16f, 0.16f, 2.5f), mats.wood);
            CreateBeam(shelter.transform, new Vector3( 1.6f, 2.6f, 0f), new Vector3(0.16f, 0.16f, 2.5f), mats.wood);

            // Sloped Plank Roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "PlankRoof";
            roof.transform.SetParent(shelter.transform);
            roof.transform.localPosition = new Vector3(0f, 2.68f, 0.05f);
            roof.transform.localRotation = Quaternion.Euler(7.2f, 0f, 0f);
            roof.transform.localScale = new Vector3(3.8f, 0.08f, 3.0f);
            roof.GetComponent<MeshRenderer>().sharedMaterial = mats.roof;

            // Hanging Brass Workshop Lantern from FantasyMedievalTown_LITE
            GameObject lantern = SpawnPrefab("Assets/FantasyMedievalTown_LITE/Prefabs/Lantern_01_LITE.prefab", shelter.transform, new Vector3(0f, 2.35f, -0.2f), Quaternion.identity, Vector3.one);
            if (lantern == null)
            {
                lantern = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lantern.name = "WorkshopLantern";
                lantern.transform.SetParent(shelter.transform);
                lantern.transform.localPosition = new Vector3(0f, 2.25f, -0.2f);
                lantern.transform.localScale = new Vector3(0.15f, 0.22f, 0.15f);
                lantern.GetComponent<MeshRenderer>().sharedMaterial = mats.lantern;
                Collider lCol = lantern.GetComponent<Collider>();
                if (lCol != null) Object.DestroyImmediate(lCol);
            }

            // Lantern Warm Point Light
            Light warmLight = lantern.GetComponentInChildren<Light>();
            if (warmLight == null) warmLight = lantern.AddComponent<Light>();
            warmLight.type = LightType.Point;
            warmLight.range = 6.5f;
            warmLight.intensity = 1.8f;
            warmLight.color = new Color(1.0f, 0.82f, 0.52f);

            // Workshop Props from LowPolyMedievalPropsLite
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/Bucket_01.prefab", shelter.transform, new Vector3(-1.45f, 0.2f, 0.35f), Quaternion.identity, Vector3.one * 1.1f);
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/Box_01.prefab", shelter.transform, new Vector3(1.45f, 0.2f, -0.3f), Quaternion.identity, Vector3.one * 0.9f);
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/Axe_01.prefab", shelter.transform, new Vector3(1.48f, 0.45f, 0.18f), Quaternion.Euler(65f, 20f, 0f), Vector3.one);
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/WoodPlank_01.prefab", shelter.transform, new Vector3(-1.58f, 0.55f, -0.85f), Quaternion.Euler(70f, 15f, 0f), Vector3.one);
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/WoodPlank_02.prefab", shelter.transform, new Vector3(-1.50f, 0.55f, -0.80f), Quaternion.Euler(72f, 10f, 0f), Vector3.one);

            // Campfire with Firewood, Stone Ring & FX_Fire_01 Particle System
            SetupCampfire(shelter.transform, new Vector3(-2.8f, 0.05f, 0.8f));
        }

        private static void CreateBeam(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.transform.SetParent(parent);
            beam.transform.localPosition = localPos;
            beam.transform.localScale = scale;
            beam.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void SetupPlayerPrefab(MaterialMaterials mats)
        {
            string prefabPath = "Assets/Player.prefab";
            GameObject playerPrefab = PrefabUtility.LoadPrefabContents(prefabPath);

            if (playerPrefab != null)
            {
                // Ensure primitive cylinder mesh on root doesn't render
                MeshRenderer rootRenderer = playerPrefab.GetComponent<MeshRenderer>();
                if (rootRenderer != null)
                {
                    rootRenderer.enabled = false;
                }

                // Ensure PlayerInteraction exists
                PlayerInteraction interaction = playerPrefab.GetComponent<PlayerInteraction>();
                if (interaction == null)
                {
                    interaction = playerPrefab.AddComponent<PlayerInteraction>();
                }

                // Ensure PlayerVisuals exists
                PlayerVisuals visuals = playerPrefab.GetComponent<PlayerVisuals>();
                if (visuals == null)
                {
                    visuals = playerPrefab.AddComponent<PlayerVisuals>();
                }

                // Camera setup
                Camera cam = playerPrefab.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    if (cam.GetComponent<AudioListener>() == null)
                    {
                        cam.gameObject.AddComponent<AudioListener>();
                    }

                    InspectionFlashlight flashlight = cam.GetComponent<InspectionFlashlight>();
                    if (flashlight == null)
                    {
                        flashlight = cam.gameObject.AddComponent<InspectionFlashlight>();
                    }

                    // Build stylized first-person arms on camera
                    GameObject fpHands = LowPolyCharacterBuilder.BuildFirstPersonHands(cam.transform, mats.skin, mats.character, mats.metal);
                    visuals.FirstPersonHands = fpHands;
                }

                // Rigged & animated third-person avatar from Blink LowPolyHumans
                string charPrefabPath = "Assets/Blink/Art/Characters/LowPoly/FREE_HumanLowPoly/Prefabs_Humans/HumanMale_Character_FREE.prefab";
                GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(charPrefabPath);
                if (charPrefab != null)
                {
                    Transform oldAvatar = playerPrefab.transform.Find("ThirdPersonAvatar");
                    if (oldAvatar != null) Object.DestroyImmediate(oldAvatar.gameObject);

                    GameObject avatar = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab, playerPrefab.transform);
                    avatar.name = "ThirdPersonAvatar";
                    avatar.transform.localPosition = new Vector3(0f, -1.0f, 0f);
                    avatar.transform.localRotation = Quaternion.identity;
                    avatar.transform.localScale = Vector3.one;

                    var controller = EnsurePlayerAnimatorController();
                    Animator anim = avatar.GetComponent<Animator>();
                    if (anim != null && controller != null)
                    {
                        anim.runtimeAnimatorController = controller;
                    }

                    visuals.ThirdPersonAvatar = avatar;
                }
                else
                {
                    // Fallback to procedural builder
                    GameObject avatar = LowPolyCharacterBuilder.BuildThirdPersonAvatar(playerPrefab, mats.skin, mats.character, mats.dirt, mats.wood);
                    visuals.ThirdPersonAvatar = avatar;
                }

                PrefabUtility.SaveAsPrefabAsset(playerPrefab, prefabPath);
                PrefabUtility.UnloadPrefabContents(playerPrefab);
                Debug.Log("[SeekingForJade] Player prefab updated with Blink HumanMale animated avatar and first-person hands.");
            }
        }

        private static void SetupCuttingStation(MaterialMaterials mats)
        {
            GameObject existingStation = GameObject.Find("CuttingWorkbench");
            if (existingStation != null)
            {
                Object.DestroyImmediate(existingStation);
            }

            GameObject bench = new GameObject("CuttingWorkbench");
            bench.transform.position = new Vector3(0f, 0f, 3.5f);

            // Tabletop
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "TableTop";
            top.transform.SetParent(bench.transform);
            top.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            top.transform.localScale = new Vector3(2.4f, 0.1f, 1.2f);
            top.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;

            // Table Legs
            Vector3[] legOffsets = new Vector3[]
            {
                new Vector3(-1.05f, 0.4f, -0.45f),
                new Vector3( 1.05f, 0.4f, -0.45f),
                new Vector3(-1.05f, 0.4f,  0.45f),
                new Vector3( 1.05f, 0.4f,  0.45f)
            };

            for (int i = 0; i < legOffsets.Length; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{i}";
                leg.transform.SetParent(bench.transform);
                leg.transform.localPosition = legOffsets[i];
                leg.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
                leg.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;
            }

            // Clamp Bed
            GameObject clamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clamp.name = "ClampBed";
            clamp.transform.SetParent(bench.transform);
            clamp.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            clamp.transform.localScale = new Vector3(0.7f, 0.12f, 0.5f);
            clamp.GetComponent<MeshRenderer>().sharedMaterial = mats.metal;

            // Rock Clamp Mount Point (sitting cleanly atop clamp bed)
            GameObject clampPoint = new GameObject("RockClampPoint");
            clampPoint.transform.SetParent(bench.transform);
            clampPoint.transform.localPosition = new Vector3(0f, 1.22f, 0f);
            clampPoint.transform.localRotation = Quaternion.identity;

            // Saw Frame Pillar
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "SawPillar";
            pillar.transform.SetParent(bench.transform);
            pillar.transform.localPosition = new Vector3(0.85f, 1.5f, 0f);
            pillar.transform.localScale = new Vector3(0.15f, 1.2f, 0.15f);
            pillar.GetComponent<MeshRenderer>().sharedMaterial = mats.metal;

            // Saw Arm (moves up and down)
            GameObject arm = new GameObject("SawArm");
            arm.transform.SetParent(bench.transform);
            arm.transform.localPosition = new Vector3(0f, 1.9f, 0f);

            GameObject armBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armBar.name = "ArmBar";
            armBar.transform.SetParent(arm.transform);
            armBar.transform.localPosition = new Vector3(0.42f, 0f, 0f);
            armBar.transform.localScale = new Vector3(0.9f, 0.08f, 0.1f);
            armBar.GetComponent<MeshRenderer>().sharedMaterial = mats.metal;

            // Circular Saw Blade
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            blade.name = "SawBlade";
            blade.transform.SetParent(arm.transform);
            blade.transform.localPosition = Vector3.zero;
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            blade.transform.localScale = new Vector3(0.75f, 0.015f, 0.75f);
            blade.GetComponent<MeshRenderer>().sharedMaterial = mats.metal;

            Collider bladeCol = blade.GetComponent<Collider>();
            if (bladeCol != null) Object.DestroyImmediate(bladeCol);

            // Blade Guard
            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "BladeGuard";
            guard.transform.SetParent(arm.transform);
            guard.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            guard.transform.localScale = new Vector3(0.12f, 0.42f, 0.8f);
            guard.GetComponent<MeshRenderer>().sharedMaterial = mats.metal;
            Collider guardCol = guard.GetComponent<Collider>();
            if (guardCol != null) Object.DestroyImmediate(guardCol);

            // Axle Hub
            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "BladeHub";
            hub.transform.SetParent(blade.transform);
            hub.transform.localPosition = Vector3.zero;
            hub.transform.localRotation = Quaternion.identity;
            hub.transform.localScale = new Vector3(0.25f, 1.8f, 0.25f);
            hub.GetComponent<MeshRenderer>().sharedMaterial = mats.metal;
            Collider hubCol = hub.GetComponent<Collider>();
            if (hubCol != null) Object.DestroyImmediate(hubCol);

            // Add CuttingSawStation Component
            CuttingSawStation station = bench.AddComponent<CuttingSawStation>();

            SerializedObject so = new SerializedObject(station);
            so.FindProperty("rockClampPoint").objectReferenceValue = clampPoint.transform;
            so.FindProperty("sawArm").objectReferenceValue = arm.transform;
            so.FindProperty("sawBlade").objectReferenceValue = blade.transform;

            if (mats.jade != null)
            {
                so.FindProperty("jadeCapMaterial").objectReferenceValue = mats.jade;
            }

            // Attach dynamic coolant mist & dust particle system
            ParticleSystem mistFx = SeekingForJade.VFX.RockVFXManager.CreateSawCuttingFX(arm.transform, new Vector3(0f, -0.35f, 0f));
            so.FindProperty("waterDustFx").objectReferenceValue = mistFx;

            AudioSource audio = bench.AddComponent<AudioSource>();
            so.FindProperty("audioSource").objectReferenceValue = audio;
            so.ApplyModifiedProperties();
        }

        private static void SetupEconomyAndQuarry(MaterialMaterials mats)
        {
            if (Object.FindAnyObjectByType<SeekingForJade.Economy.PlayerWallet>() == null)
            {
                GameObject walletObj = new GameObject("PlayerWallet");
                walletObj.AddComponent<SeekingForJade.Economy.PlayerWallet>();
            }

            RockData riverRock = AssetDatabase.LoadAssetAtPath<RockData>("Assets/Settings/HpakantRiverBoulder.asset");
            RockData tapeRock = AssetDatabase.LoadAssetAtPath<RockData>("Assets/Settings/TapeWrappedMysteryBoulder.asset");
            RockData moShaRock = AssetDatabase.LoadAssetAtPath<RockData>("Assets/Settings/MoShaBlackBoulder.asset");
            RockData whiteSaltRock = AssetDatabase.LoadAssetAtPath<RockData>("Assets/Settings/WhiteSaltBoulder.asset");

            // Setup Mining Quarry
            GameObject existingQuarry = GameObject.Find("MiningQuarry");
            if (existingQuarry != null) Object.DestroyImmediate(existingQuarry);

            GameObject quarry = new GameObject("MiningQuarry");
            quarry.transform.position = new Vector3(-3.8f, 0f, 2.5f);

            // Watertight organic bedrock outcrop
            GameObject mound = new GameObject("RockMound");
            mound.transform.SetParent(quarry.transform);
            mound.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            mound.transform.localScale = Vector3.one;

            MeshFilter qmf = mound.AddComponent<MeshFilter>();
            MeshRenderer qmr = mound.AddComponent<MeshRenderer>();
            MeshCollider qmc = mound.AddComponent<MeshCollider>();
            Mesh outcropMesh = LowPolyMeshGenerator.GenerateQuarryRockOutcrop(888);
            qmf.sharedMesh = outcropMesh;
            qmc.sharedMesh = outcropMesh;
            qmr.sharedMaterial = mats.dirt;

            // Decorative low-poly boulders around quarry perimeter
            for (int i = 0; i < 3; i++)
            {
                float ang = i * 2.1f;
                GameObject qb = new GameObject($"QuarryBoulder_{i}");
                qb.transform.SetParent(quarry.transform);
                qb.transform.localPosition = new Vector3(Mathf.Cos(ang) * 1.5f, 0.1f, Mathf.Sin(ang) * 1.5f);
                MeshFilter bmf = qb.AddComponent<MeshFilter>();
                MeshRenderer bmr = qb.AddComponent<MeshRenderer>();
                bmf.sharedMesh = LowPolyMeshGenerator.GenerateLowPolyBoulder(770 + i, 0.45f);
                bmr.sharedMaterial = mats.dirt;
            }

            // Spawn point for mined rocks
            GameObject spawnPt = new GameObject("RockSpawnPoint");
            spawnPt.transform.SetParent(quarry.transform);
            spawnPt.transform.localPosition = new Vector3(0f, 0.85f, 0f);

            var mining = quarry.AddComponent<SeekingForJade.Environment.MiningPile>();
            SerializedObject soMine = new SerializedObject(mining);
            soMine.FindProperty("rawRockData").objectReferenceValue = riverRock;
            soMine.FindProperty("rockSpawnPoint").objectReferenceValue = spawnPt.transform;
            soMine.ApplyModifiedProperties();

            // Setup Trader Stall with Physical Market Inspection Table
            GameObject existingTrader = GameObject.Find("TraderStall");
            if (existingTrader != null) Object.DestroyImmediate(existingTrader);

            GameObject trader = new GameObject("TraderStall");
            trader.transform.position = new Vector3(3.8f, 0f, 2.5f);

            // Wide counter table for scale + 4 candidate display slots
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "CounterTable";
            counter.transform.SetParent(trader.transform);
            counter.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            counter.transform.localScale = new Vector3(3.2f, 0.1f, 1.5f);
            counter.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;

            // Table legs
            for (int i = 0; i < 4; i++)
            {
                float lx = (i % 2 == 0) ? -1.45f : 1.45f;
                float lz = (i < 2) ? -0.6f : 0.6f;
                GameObject tLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tLeg.name = $"TraderLeg_{i}";
                tLeg.transform.SetParent(trader.transform);
                tLeg.transform.localPosition = new Vector3(lx, 0.4f, lz);
                tLeg.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
                tLeg.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;
            }

            // Left Side: Appraisal Scale Plate (Place sliced jade here to sell)
            GameObject scale = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scale.name = "AppraisalScalePlate";
            scale.transform.SetParent(trader.transform);
            scale.transform.localPosition = new Vector3(-1.05f, 0.92f, 0f);
            scale.transform.localScale = new Vector3(0.72f, 0.02f, 0.72f);
            scale.GetComponent<MeshRenderer>().sharedMaterial = mats.metal;

            // Right Side: 4 Market Inspection Display Slots in a clean presentation row
            Vector3[] slotPositions = new Vector3[]
            {
                new Vector3(-0.65f, 0.98f, 0.05f),
                new Vector3(-0.10f, 0.98f, 0.05f),
                new Vector3( 0.45f, 0.98f, 0.05f),
                new Vector3( 1.00f, 0.98f, 0.05f)
            };

            Transform[] slotTransforms = new Transform[slotPositions.Length];
            for (int i = 0; i < slotPositions.Length; i++)
            {
                GameObject slotObj = new GameObject($"MarketSlot_{i}");
                slotObj.transform.SetParent(trader.transform);
                slotObj.transform.localPosition = slotPositions[i];
                slotTransforms[i] = slotObj.transform;

                // Little wooden display coaster
                GameObject coaster = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coaster.name = $"DisplayCoaster_{i}";
                coaster.transform.SetParent(trader.transform);
                coaster.transform.localPosition = slotPositions[i] - Vector3.up * 0.06f;
                coaster.transform.localScale = new Vector3(0.55f, 0.02f, 0.55f);
                coaster.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;
                Collider cCol = coaster.GetComponent<Collider>();
                if (cCol != null) Object.DestroyImmediate(cCol);
            }

            // Spawn Master Chen (Jade Merchant) character model standing proudly behind the counter!
            SeekingForJade.Player.LowPolyCharacterBuilder.BuildMerchantNPC(
                trader.transform,
                new Vector3(0.15f, 0f, 0.95f),
                Quaternion.Euler(0f, 180f, 0f),
                mats.skin,
                mats.merchantSilk,
                mats.gold,
                mats.darkHair,
                mats.jade
            );

            var npc = trader.AddComponent<SeekingForJade.Economy.JadeTraderNPC>();
            SerializedObject soNpc = new SerializedObject(npc);
            soNpc.FindProperty("scaleZone").objectReferenceValue = scale.transform;

            // Catalog data
            List<RockData> catalog = new List<RockData>();
            List<int> prices = new List<int>();

            if (riverRock != null)     { catalog.Add(riverRock);     prices.Add(150); }
            if (whiteSaltRock != null) { catalog.Add(whiteSaltRock); prices.Add(300); }
            if (moShaRock != null)     { catalog.Add(moShaRock);     prices.Add(450); }
            if (tapeRock != null)      { catalog.Add(tapeRock);      prices.Add(250); }

            SerializedProperty bouldersProp = soNpc.FindProperty("availableBoulders");
            bouldersProp.arraySize = catalog.Count;
            for (int i = 0; i < catalog.Count; i++)
            {
                bouldersProp.GetArrayElementAtIndex(i).objectReferenceValue = catalog[i];
            }

            SerializedProperty pricesProp = soNpc.FindProperty("boulderPrices");
            pricesProp.arraySize = prices.Count;
            for (int i = 0; i < prices.Count; i++)
            {
                pricesProp.GetArrayElementAtIndex(i).intValue = prices[i];
            }

            SerializedProperty slotsProp = soNpc.FindProperty("displaySlots");
            slotsProp.arraySize = slotTransforms.Length;
            for (int i = 0; i < slotTransforms.Length; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotTransforms[i];
            }

            soNpc.ApplyModifiedProperties();

            // Populate initial display rocks immediately in editor
            npc.InitializeDisplayTable();

            // Hanging Town Lantern over Master Chen's stall
            GameObject stallLantern = SpawnPrefab("Assets/FantasyMedievalTown_LITE/Prefabs/Lantern_01_LITE.prefab", trader.transform, new Vector3(0f, 2.3f, 0f), Quaternion.identity, Vector3.one);
            if (stallLantern != null)
            {
                Light ml = stallLantern.GetComponentInChildren<Light>();
                if (ml == null) ml = stallLantern.AddComponent<Light>();
                ml.type = LightType.Point;
                ml.range = 5.5f;
                ml.intensity = 1.6f;
                ml.color = new Color(1.0f, 0.85f, 0.55f);
            }

            // Props on Master Chen's counter table (Coins, Cups, Jugs, Merchant Lockbox)
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/Coin_01.prefab", trader.transform, new Vector3(-1.35f, 0.92f, 0.25f), Quaternion.identity, Vector3.one);
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/Coin_02.prefab", trader.transform, new Vector3(-1.25f, 0.92f, 0.32f), Quaternion.Euler(0f, 35f, 0f), Vector3.one);
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/Coin_03.prefab", trader.transform, new Vector3(-1.42f, 0.92f, 0.15f), Quaternion.Euler(0f, -20f, 0f), Vector3.one);
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/Jug_01.prefab", trader.transform, new Vector3(-1.4f, 0.92f, -0.35f), Quaternion.identity, Vector3.one * 0.9f);
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/Cup_01.prefab", trader.transform, new Vector3(-1.2f, 0.92f, -0.4f), Quaternion.identity, Vector3.one * 0.9f);
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/Box_01.prefab", trader.transform, new Vector3(1.35f, 0.92f, -0.3f), Quaternion.identity, Vector3.one * 0.85f);

            // Flanking Barrel and Flower Pot
            SpawnPrefab("Assets/FantasyMedievalTown_LITE/Prefabs/Barrel_01_LITE.prefab", trader.transform, new Vector3(1.85f, 0f, 0.6f), Quaternion.identity, Vector3.one);
            SpawnPrefab("Assets/FantasyMedievalTown_LITE/Prefabs/FlowerPot_03_LITE.prefab", trader.transform, new Vector3(-1.85f, 0f, -0.6f), Quaternion.identity, Vector3.one);

            // Fencing along perimeter behind and beside stall
            SpawnPrefab("Assets/FantasyMedievalTown_LITE/Prefabs/Fence_01_LITE.prefab", trader.transform, new Vector3(2.4f, 0f, 0.4f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            SpawnPrefab("Assets/FantasyMedievalTown_LITE/Prefabs/Fence_01_LITE.prefab", trader.transform, new Vector3(2.4f, 0f, -1.6f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        }

        private static void SpawnTestRocks()
        {
            GameObject existingGroup = GameObject.Find("TestRocks");
            if (existingGroup != null) Object.DestroyImmediate(existingGroup);

            GameObject rockGroup = new GameObject("TestRocks");

            RockData riverRock = AssetDatabase.LoadAssetAtPath<RockData>("Assets/Settings/HpakantRiverBoulder.asset");
            RockData tapeRock = AssetDatabase.LoadAssetAtPath<RockData>("Assets/Settings/TapeWrappedMysteryBoulder.asset");
            RockData moShaRock = AssetDatabase.LoadAssetAtPath<RockData>("Assets/Settings/MoShaBlackBoulder.asset");
            RockData whiteSaltRock = AssetDatabase.LoadAssetAtPath<RockData>("Assets/Settings/WhiteSaltBoulder.asset");
            Material jadeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Jade_Internal.mat");

            // 1. TAPE-WRAPPED MYSTERY BOULDER (On workbench left!)
            // Completely wrapped in yellow tape, flashlight inspection is completely blocked!
            if (tapeRock != null)
            {
                GameObject rTape = ProceduralRockGenerator.CreateRockGameObject(tapeRock, 77777, new Vector3(-0.78f, 1.08f, 3.4f));
                rTape.transform.SetParent(rockGroup.transform);
                rTape.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
                rTape.name = "Boulder_TapeWrapped_Mystery_77777";
            }

            // 2. CLASSIC HPAKANT RIVER BOULDER (On workbench far left edge)
            if (riverRock != null)
            {
                GameObject rRiver = ProceduralRockGenerator.CreateRockGameObject(riverRock, 1024, new Vector3(-0.45f, 1.08f, 3.65f));
                rRiver.transform.SetParent(rockGroup.transform);
                rRiver.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
                rRiver.name = "Boulder_Hpakant_River_1024";
            }

            // 3. SLICED MO-SHA BLACK BOULDER (Right under saw blade - showcasing internal jade face)
            if (moShaRock != null)
            {
                GameObject rSliced = ProceduralRockGenerator.CreateRockGameObject(moShaRock, 99999, new Vector3(0.15f, 1.10f, 3.5f));
                rSliced.name = "Boulder_MoSha_Black_99999";

                var sliceResult = MeshSlicer.Slice(rSliced, rSliced.transform.position, Vector3.right, jadeMat);
                if (sliceResult.success)
                {
                    GameObject halfA = sliceResult.positiveSideObject;
                    GameObject halfB = sliceResult.negativeSideObject;

                    halfA.transform.SetParent(rockGroup.transform);
                    halfB.transform.SetParent(rockGroup.transform);

                    ProceduralRock rockA = halfA.GetComponent<ProceduralRock>();
                    if (rockA != null)
                    {
                        rockA.MarkAsSawCut();
                    }

                    ProceduralRock rockB = halfB.GetComponent<ProceduralRock>();
                    if (rockB == null) rockB = halfB.AddComponent<ProceduralRock>();
                    rockB.CopyFromParent(rockA, rockA.WeightKg * 0.5f);
                    rockB.MarkAsSawCut();

                    // Position halves facing player showcasing the rich green jade cross-section
                    halfA.transform.position = new Vector3(0.26f, 1.10f, 3.5f);
                    halfA.transform.rotation = Quaternion.Euler(15f, -80f, 0f);

                    halfB.transform.position = new Vector3(-0.04f, 1.10f, 3.5f);
                    halfB.transform.rotation = Quaternion.Euler(15f, 80f, 0f);
                }
                else
                {
                    rSliced.transform.SetParent(rockGroup.transform);
                }
            }

            // 4. SMASHED MULTI-FRAGMENT BOULDER (On floor left of workbench)
            // Demonstrating multi-chunk jagged fracture & -60% value penalty
            if (riverRock != null)
            {
                GameObject rSmash = ProceduralRockGenerator.CreateRockGameObject(riverRock, 88812, new Vector3(-1.4f, 0.45f, 2.3f));
                rSmash.name = "Boulder_Smashed_Crude_88812";
                rSmash.transform.SetParent(rockGroup.transform);
                RockImpactBreaker breaker = rSmash.GetComponent<RockImpactBreaker>();
                if (breaker != null)
                {
                    breaker.BreakOnImpact(rSmash.transform.position, Vector3.up);

                    // Separate the shattered pieces in edit mode so they don't overlap
                    rSmash.transform.position = new Vector3(-1.18f, 0.28f, 2.2f);
                    rSmash.transform.rotation = Quaternion.Euler(25f, 50f, 0f);

                    foreach (Transform child in rockGroup.transform)
                    {
                        if (child.name.Contains("88812") && child != rSmash.transform)
                        {
                            if (child.name.Contains("Shard"))
                            {
                                child.position = new Vector3(-1.40f, 0.20f, 1.95f);
                                child.rotation = Quaternion.Euler(15f, 110f, 0f);
                            }
                            else
                            {
                                child.position = new Vector3(-1.62f, 0.28f, 2.2f);
                                child.rotation = Quaternion.Euler(-25f, -60f, 0f);
                            }
                        }
                    }
                }
            }

            // 5. Raw Boulder at Mining Quarry
            if (riverRock != null)
            {
                GameObject rQuarry = ProceduralRockGenerator.CreateRockGameObject(riverRock, 44444, new Vector3(-3.8f, 0.95f, 2.5f));
                rQuarry.transform.SetParent(rockGroup.transform);
                rQuarry.name = "Boulder_Quarry_44444";
            }
        }

        public static GameObject SpawnPrefab(string assetPath, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.transform.localPosition = localPos;
                instance.transform.localRotation = localRot;
                instance.transform.localScale = scale;
                return instance;
            }
            return null;
        }

        private static void SetupCampfire(Transform parent, Vector3 localPos)
        {
            GameObject fireGroup = new GameObject("Campfire_Hearth");
            fireGroup.transform.SetParent(parent);
            fireGroup.transform.localPosition = localPos;

            // Firewood pile
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/Firewood_01.prefab", fireGroup.transform, Vector3.zero, Quaternion.identity, Vector3.one);

            // Surrounding stone ring
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Vector3 sPos = new Vector3(Mathf.Cos(a) * 0.48f, 0f, Mathf.Sin(a) * 0.48f);
                SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/Stone_01.prefab", fireGroup.transform, sPos, Quaternion.Euler(0f, i * 60f, 0f), Vector3.one * 0.65f);
            }

            // FX_Fire_01 Particle System
            SpawnPrefab("Assets/LowPolyMedievalPropsLite/Prefabs/FX/FX_Fire_01.prefab", fireGroup.transform, new Vector3(0f, 0.05f, 0f), Quaternion.identity, Vector3.one);

            // Warm flickering campfire point light
            GameObject fireLightObj = new GameObject("CampfireLight");
            fireLightObj.transform.SetParent(fireGroup.transform);
            fireLightObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            Light fl = fireLightObj.AddComponent<Light>();
            fl.type = LightType.Point;
            fl.range = 8.5f;
            fl.intensity = 2.2f;
            fl.color = new Color(1.0f, 0.65f, 0.28f);
        }

        private static RuntimeAnimatorController EnsurePlayerAnimatorController()
        {
            string animPath = "Assets/Settings/PlayerAnimatorController.controller";
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(animPath);
            if (controller == null)
            {
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(animPath);
                controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
                controller.AddParameter("Mining", AnimatorControllerParameterType.Trigger);

                AnimationClip idleClip = GetClipFromAsset("Assets/Blink/Art/Animations/Animations_Starter_Pack/Movement/Idle.fbx");
                AnimationClip runClip = GetClipFromAsset("Assets/Blink/Art/Animations/Animations_Starter_Pack/Movement/RunForward.fbx");
                AnimationClip miningClip = GetClipFromAsset("Assets/Blink/Art/Animations/Animations_Starter_Pack/Gathering/MiningLoop.fbx");

                var rootSm = controller.layers[0].stateMachine;

                var idleState = rootSm.AddState("Idle");
                if (idleClip != null) idleState.motion = idleClip;

                var runState = rootSm.AddState("Run");
                if (runClip != null) runState.motion = runClip;

                var idleToRun = idleState.AddTransition(runState);
                idleToRun.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "Speed");
                idleToRun.hasExitTime = false;
                idleToRun.duration = 0.15f;

                var runToIdle = runState.AddTransition(idleState);
                runToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Speed");
                runToIdle.hasExitTime = false;
                runToIdle.duration = 0.15f;

                if (miningClip != null)
                {
                    var miningState = rootSm.AddState("Mining");
                    miningState.motion = miningClip;

                    var anyToMining = rootSm.AddAnyStateTransition(miningState);
                    anyToMining.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Mining");
                    anyToMining.hasExitTime = false;
                    anyToMining.duration = 0.1f;

                    var miningToIdle = miningState.AddTransition(idleState);
                    miningToIdle.hasExitTime = true;
                    miningToIdle.exitTime = 0.85f;
                    miningToIdle.duration = 0.2f;
                }

                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
            return controller;
        }

        private static AnimationClip GetClipFromAsset(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }
    }
}
#endif
