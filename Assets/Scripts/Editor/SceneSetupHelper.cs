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

            // Seeded tree ring around the clearing
            var rng = new System.Random(42);
            int treeCount = 32;
            for (int i = 0; i < treeCount; i++)
            {
                float angle = i * (Mathf.PI * 2f / treeCount) + ((float)rng.NextDouble() - 0.5f) * 0.25f;
                float radius = Mathf.Lerp(9.5f, 18.0f, (float)rng.NextDouble());

                float x = Mathf.Cos(angle) * radius;
                float z = 3.0f + Mathf.Sin(angle) * radius;

                // Sample terrain height roughly
                float dist = Mathf.Sqrt(x * x + (z - 3f) * (z - 3f));
                float y = dist > 7.5f ? Mathf.Pow((dist - 7.5f) / 12f, 1.8f) * 3.5f : 0f;

                GameObject tree = new GameObject($"Tree_{i}");
                tree.transform.SetParent(treeGroup.transform);
                tree.transform.position = new Vector3(x, y, z);
                tree.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

                MeshFilter mf = tree.AddComponent<MeshFilter>();
                MeshRenderer mr = tree.AddComponent<MeshRenderer>();

                bool isPine = rng.NextDouble() > 0.35;
                if (isPine)
                {
                    float treeHeight = Mathf.Lerp(4.0f, 6.5f, (float)rng.NextDouble());
                    mf.sharedMesh = LowPolyMeshGenerator.GeneratePineTree(100 + i, treeHeight);
                }
                else
                {
                    float treeHeight = Mathf.Lerp(3.5f, 5.0f, (float)rng.NextDouble());
                    mf.sharedMesh = LowPolyMeshGenerator.GenerateDeciduousTree(200 + i, treeHeight);
                }

                mr.sharedMaterials = new Material[] { mats.wood, mats.foliage };

                // Tree trunk collider
                CapsuleCollider col = tree.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 1.2f, 0f);
                col.radius = 0.35f;
                col.height = 2.4f;
            }

            // Decorative low-poly boulders around clearing perimeter
            for (int i = 0; i < 10; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = Mathf.Lerp(7.0f, 12.0f, (float)rng.NextDouble());
                Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0.1f, 3.0f + Mathf.Sin(angle) * dist);

                GameObject boulder = new GameObject($"DecoBoulder_{i}");
                boulder.transform.SetParent(treeGroup.transform);
                boulder.transform.position = pos;
                boulder.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

                MeshFilter mf = boulder.AddComponent<MeshFilter>();
                MeshRenderer mr = boulder.AddComponent<MeshRenderer>();
                mf.sharedMesh = LowPolyMeshGenerator.GenerateLowPolyBoulder(300 + i, Mathf.Lerp(0.5f, 1.1f, (float)rng.NextDouble()));
                mr.sharedMaterial = mats.dirt;
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

            // Hanging Brass Workshop Lantern
            GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lantern.name = "WorkshopLantern";
            lantern.transform.SetParent(shelter.transform);
            lantern.transform.localPosition = new Vector3(0f, 2.25f, -0.2f);
            lantern.transform.localScale = new Vector3(0.15f, 0.22f, 0.15f);
            lantern.GetComponent<MeshRenderer>().sharedMaterial = mats.lantern;

            Collider lCol = lantern.GetComponent<Collider>();
            if (lCol != null) Object.DestroyImmediate(lCol);

            // Lantern Warm Point Light
            Light warmLight = lantern.AddComponent<Light>();
            warmLight.type = LightType.Point;
            warmLight.range = 6.0f;
            warmLight.intensity = 1.5f;
            warmLight.color = new Color(1.0f, 0.78f, 0.48f);
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

                // Build stylized third-person avatar for multiplayer
                GameObject avatar = LowPolyCharacterBuilder.BuildThirdPersonAvatar(playerPrefab, mats.skin, mats.character, mats.dirt, mats.wood);
                visuals.ThirdPersonAvatar = avatar;

                PrefabUtility.SaveAsPrefabAsset(playerPrefab, prefabPath);
                PrefabUtility.UnloadPrefabContents(playerPrefab);
                Debug.Log("[SeekingForJade] Player prefab updated with LowPolyCharacterBuilder hands and avatar.");
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

            // Visual faceted rock pile
            GameObject mound = new GameObject("RockMound");
            mound.transform.SetParent(quarry.transform);
            mound.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            mound.transform.localScale = new Vector3(2.5f, 0.85f, 2.5f);
            MeshFilter qmf = mound.AddComponent<MeshFilter>();
            MeshRenderer qmr = mound.AddComponent<MeshRenderer>();
            qmf.sharedMesh = LowPolyMeshGenerator.GenerateLowPolyBoulder(888, 1.0f);
            qmr.sharedMaterial = mats.dirt;

            // Spawn point for mined rocks
            GameObject spawnPt = new GameObject("RockSpawnPoint");
            spawnPt.transform.SetParent(quarry.transform);
            spawnPt.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            var mining = quarry.AddComponent<SeekingForJade.Environment.MiningPile>();
            SerializedObject soMine = new SerializedObject(mining);
            soMine.FindProperty("rawRockData").objectReferenceValue = riverRock;
            soMine.FindProperty("rockSpawnPoint").objectReferenceValue = spawnPt.transform;
            soMine.ApplyModifiedProperties();

            // Setup Trader Stall
            GameObject existingTrader = GameObject.Find("TraderStall");
            if (existingTrader != null) Object.DestroyImmediate(existingTrader);

            GameObject trader = new GameObject("TraderStall");
            trader.transform.position = new Vector3(3.8f, 0f, 2.5f);

            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "CounterTable";
            counter.transform.SetParent(trader.transform);
            counter.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            counter.transform.localScale = new Vector3(2.2f, 0.1f, 1.2f);
            counter.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;

            // Table legs
            for (int i = 0; i < 4; i++)
            {
                float lx = (i % 2 == 0) ? -0.95f : 0.95f;
                float lz = (i < 2) ? -0.45f : 0.45f;
                GameObject tLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tLeg.name = $"TraderLeg_{i}";
                tLeg.transform.SetParent(trader.transform);
                tLeg.transform.localPosition = new Vector3(lx, 0.4f, lz);
                tLeg.transform.localScale = new Vector3(0.1f, 0.8f, 0.1f);
                tLeg.GetComponent<MeshRenderer>().sharedMaterial = mats.wood;
            }

            // Scale plate
            GameObject scale = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scale.name = "AppraisalScalePlate";
            scale.transform.SetParent(trader.transform);
            scale.transform.localPosition = new Vector3(-0.55f, 0.92f, 0f);
            scale.transform.localScale = new Vector3(0.65f, 0.02f, 0.65f);
            scale.GetComponent<MeshRenderer>().sharedMaterial = mats.metal;

            // Shop purchase spawn point
            GameObject buySpawn = new GameObject("PurchaseSpawnPoint");
            buySpawn.transform.SetParent(trader.transform);
            buySpawn.transform.localPosition = new Vector3(0.55f, 1.05f, 0f);

            var npc = trader.AddComponent<SeekingForJade.Economy.JadeTraderNPC>();
            SerializedObject soNpc = new SerializedObject(npc);
            soNpc.FindProperty("scaleZone").objectReferenceValue = scale.transform;
            soNpc.FindProperty("purchaseSpawnPoint").objectReferenceValue = buySpawn.transform;

            // Add all 4 boulder varieties to trader catalogue
            List<RockData> catalog = new List<RockData>();
            List<int> prices = new List<int>();

            if (riverRock != null) { catalog.Add(riverRock); prices.Add(250); }
            if (tapeRock != null) { catalog.Add(tapeRock); prices.Add(400); }
            if (whiteSaltRock != null) { catalog.Add(whiteSaltRock); prices.Add(500); }
            if (moShaRock != null) { catalog.Add(moShaRock); prices.Add(600); }

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

            soNpc.ApplyModifiedProperties();
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
                        rockA.MarkAsSliced();
                        rockA.ApplyVisualProperties();
                    }

                    ProceduralRock rockB = halfB.GetComponent<ProceduralRock>();
                    if (rockB == null) rockB = halfB.AddComponent<ProceduralRock>();
                    rockB.CopyFromParent(rockA, rockA.WeightKg * 0.5f);
                    rockB.MarkAsSliced();
                    rockB.ApplyVisualProperties();

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

            // 4. WHITE SALT BOULDER (On Trader Counter display)
            if (whiteSaltRock != null)
            {
                GameObject rWhite = ProceduralRockGenerator.CreateRockGameObject(whiteSaltRock, 33333, new Vector3(3.4f, 1.05f, 2.5f));
                rWhite.transform.SetParent(rockGroup.transform);
                rWhite.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
                rWhite.name = "Boulder_WhiteSalt_33333";
            }

            // 5. Raw Boulder at Mining Quarry
            if (riverRock != null)
            {
                GameObject rQuarry = ProceduralRockGenerator.CreateRockGameObject(riverRock, 44444, new Vector3(-3.8f, 0.75f, 2.5f));
                rQuarry.transform.SetParent(rockGroup.transform);
                rQuarry.name = "Boulder_Quarry_44444";
            }
        }
    }
}
#endif
