#if UNITY_EDITOR
using SeekingForJade.Jade;
using SeekingForJade.Player;
using SeekingForJade.Tools;
using SeekingForJade.Workstations;
using SeekingForJade.Slicing;
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

            // 1. Dark Floor & Environment Polish
            SetupFloorAndEnvironment();

            // 2. Configure Player Prefab
            SetupPlayerPrefab();

            // 3. Setup Cutting Workbench in Scene
            SetupCuttingStation();

            // 4. Setup Mining Quarry & Trader Stall
            SetupEconomyAndQuarry();

            // 5. Spawn Test Rocks
            SpawnTestRocks();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green><b>[SeekingForJade]</b></color> Prototype scene setup complete! Dark floor, workbench, quarry, trader and rocks spawned.");
        }

        private static void SetupFloorAndEnvironment()
        {
            // Dark Slate Floor Material
            string floorMatPath = "Assets/Materials/M_Dark_Floor.mat";
            Material darkFloorMat = AssetDatabase.LoadAssetAtPath<Material>(floorMatPath);
            if (darkFloorMat == null)
            {
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                darkFloorMat = new Material(litShader);
                darkFloorMat.SetColor("_BaseColor", new Color(0.12f, 0.13f, 0.16f, 1.0f));
                darkFloorMat.SetFloat("_Smoothness", 0.18f);
                AssetDatabase.CreateAsset(darkFloorMat, floorMatPath);
            }
            else
            {
                darkFloorMat.SetColor("_BaseColor", new Color(0.12f, 0.13f, 0.16f, 1.0f));
                darkFloorMat.SetFloat("_Smoothness", 0.18f);
                EditorUtility.SetDirty(darkFloorMat);
            }

            // Find or configure floor Plane
            GameObject plane = GameObject.Find("Plane");
            if (plane != null)
            {
                plane.transform.position = Vector3.zero;
                plane.transform.localScale = new Vector3(6.0f, 1.0f, 6.0f);
                MeshRenderer planeRenderer = plane.GetComponent<MeshRenderer>();
                if (planeRenderer != null)
                {
                    planeRenderer.sharedMaterial = darkFloorMat;
                }
            }

            // Relocate starter multiplayer Cube away from workbench
            GameObject cube = GameObject.Find("Cube");
            if (cube != null)
            {
                cube.transform.position = new Vector3(-2.2f, 0.5f, 0.8f);
            }
        }

        private static void SetupPlayerPrefab()
        {
            string prefabPath = "Assets/Player.prefab";
            GameObject playerPrefab = PrefabUtility.LoadPrefabContents(prefabPath);

            if (playerPrefab != null)
            {
                // Ensure PlayerInteraction exists
                PlayerInteraction interaction = playerPrefab.GetComponent<PlayerInteraction>();
                if (interaction == null)
                {
                    interaction = playerPrefab.AddComponent<PlayerInteraction>();
                }

                // Ensure PlayerCamera has InspectionFlashlight and AudioListener
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

                    // Ensure spotlight child exists on camera
                    Light spot = cam.GetComponentInChildren<Light>();
                    if (spot == null)
                    {
                        GameObject spotObj = new GameObject("InspectionLight");
                        spotObj.transform.SetParent(cam.transform);
                        spotObj.transform.localPosition = new Vector3(0.2f, -0.15f, 0.1f);
                        spotObj.transform.localRotation = Quaternion.identity;

                        spot = spotObj.AddComponent<Light>();
                        spot.type = LightType.Spot;
                        spot.spotAngle = 25f;
                        spot.innerSpotAngle = 12f;
                        spot.range = 8f;
                        spot.intensity = 1800f;
                        spot.color = new Color(1.0f, 0.88f, 0.55f);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(playerPrefab, prefabPath);
                PrefabUtility.UnloadPrefabContents(playerPrefab);
                Debug.Log("[SeekingForJade] Player prefab updated with interaction & inspection flashlight.");
            }
        }

        private static void SetupCuttingStation()
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

            // Table Legs
            Vector3[] legOffsets = new Vector3[]
            {
                new Vector3(-1.05f, 0.4f, -0.45f),
                new Vector3(1.05f, 0.4f, -0.45f),
                new Vector3(-1.05f, 0.4f, 0.45f),
                new Vector3(1.05f, 0.4f, 0.45f)
            };

            for (int i = 0; i < legOffsets.Length; i++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{i}";
                leg.transform.SetParent(bench.transform);
                leg.transform.localPosition = legOffsets[i];
                leg.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
            }

            // Clamp Bed
            GameObject clamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clamp.name = "ClampBed";
            clamp.transform.SetParent(bench.transform);
            clamp.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            clamp.transform.localScale = new Vector3(0.7f, 0.12f, 0.5f);

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

            // Saw Arm (moves up and down)
            GameObject arm = new GameObject("SawArm");
            arm.transform.SetParent(bench.transform);
            arm.transform.localPosition = new Vector3(0f, 1.9f, 0f);

            GameObject armBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armBar.name = "ArmBar";
            armBar.transform.SetParent(arm.transform);
            armBar.transform.localPosition = new Vector3(0.42f, 0f, 0f);
            armBar.transform.localScale = new Vector3(0.9f, 0.08f, 0.1f);

            // Circular Saw Blade: disc aligned in Y-Z plane (cutting plane), axle along X
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            blade.name = "SawBlade";
            blade.transform.SetParent(arm.transform);
            blade.transform.localPosition = Vector3.zero;
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            blade.transform.localScale = new Vector3(0.75f, 0.015f, 0.75f);

            // Disable blade collider so it doesn't bump objects
            Collider bladeCol = blade.GetComponent<Collider>();
            if (bladeCol != null) Object.DestroyImmediate(bladeCol);

            // Blade Guard (semi-circular protective housing over top half of blade)
            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "BladeGuard";
            guard.transform.SetParent(arm.transform);
            guard.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            guard.transform.localScale = new Vector3(0.12f, 0.42f, 0.8f);
            Collider guardCol = guard.GetComponent<Collider>();
            if (guardCol != null) Object.DestroyImmediate(guardCol);

            // Axle Hub (shaft collar on blade center)
            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "BladeHub";
            hub.transform.SetParent(blade.transform);
            hub.transform.localPosition = Vector3.zero;
            hub.transform.localRotation = Quaternion.identity;
            hub.transform.localScale = new Vector3(0.25f, 1.8f, 0.25f);
            Collider hubCol = hub.GetComponent<Collider>();
            if (hubCol != null) Object.DestroyImmediate(hubCol);

            // Add CuttingSawStation Component
            CuttingSawStation station = bench.AddComponent<CuttingSawStation>();

            // Wire fields via SerializedObject
            SerializedObject so = new SerializedObject(station);
            so.FindProperty("rockClampPoint").objectReferenceValue = clampPoint.transform;
            so.FindProperty("sawArm").objectReferenceValue = arm.transform;
            so.FindProperty("sawBlade").objectReferenceValue = blade.transform;

            Material jadeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Jade_Internal.mat");
            if (jadeMat != null)
            {
                so.FindProperty("jadeCapMaterial").objectReferenceValue = jadeMat;
            }

            AudioSource audio = bench.AddComponent<AudioSource>();
            so.FindProperty("audioSource").objectReferenceValue = audio;
            so.ApplyModifiedProperties();
        }

        private static void SetupEconomyAndQuarry()
        {
            // Ensure PlayerWallet exists in scene
            if (Object.FindAnyObjectByType<SeekingForJade.Economy.PlayerWallet>() == null)
            {
                GameObject walletObj = new GameObject("PlayerWallet");
                walletObj.AddComponent<SeekingForJade.Economy.PlayerWallet>();
            }

            RockData rockData = AssetDatabase.LoadAssetAtPath<RockData>("Assets/Settings/HpakantRiverBoulder.asset");

            // Setup Mining Quarry Pile
            GameObject existingQuarry = GameObject.Find("MiningQuarry");
            if (existingQuarry != null) Object.DestroyImmediate(existingQuarry);

            GameObject quarry = new GameObject("MiningQuarry");
            quarry.transform.position = new Vector3(-3.8f, 0f, 2.5f);

            // Visual mound
            GameObject mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mound.name = "RockMound";
            mound.transform.SetParent(quarry.transform);
            mound.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            mound.transform.localScale = new Vector3(2.2f, 0.8f, 2.2f);
            Material crustMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Rock_Crust.mat");
            if (crustMat != null) mound.GetComponent<MeshRenderer>().sharedMaterial = crustMat;

            // Spawn point for mined rocks
            GameObject spawnPt = new GameObject("RockSpawnPoint");
            spawnPt.transform.SetParent(quarry.transform);
            spawnPt.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            var mining = quarry.AddComponent<SeekingForJade.Environment.MiningPile>();
            SerializedObject soMine = new SerializedObject(mining);
            soMine.FindProperty("rawRockData").objectReferenceValue = rockData;
            soMine.FindProperty("rockSpawnPoint").objectReferenceValue = spawnPt.transform;
            soMine.ApplyModifiedProperties();

            // Setup Trader Counter
            GameObject existingTrader = GameObject.Find("TraderStall");
            if (existingTrader != null) Object.DestroyImmediate(existingTrader);

            GameObject trader = new GameObject("TraderStall");
            trader.transform.position = new Vector3(3.8f, 0f, 2.5f);

            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "CounterTable";
            counter.transform.SetParent(trader.transform);
            counter.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            counter.transform.localScale = new Vector3(2.2f, 0.1f, 1.2f);

            // Scale plate (place sliced jade here to sell)
            GameObject scale = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scale.name = "AppraisalScalePlate";
            scale.transform.SetParent(trader.transform);
            scale.transform.localPosition = new Vector3(-0.55f, 0.92f, 0f);
            scale.transform.localScale = new Vector3(0.65f, 0.02f, 0.65f);

            // Shop purchase spawn point
            GameObject buySpawn = new GameObject("PurchaseSpawnPoint");
            buySpawn.transform.SetParent(trader.transform);
            buySpawn.transform.localPosition = new Vector3(0.55f, 1.05f, 0f);

            var npc = trader.AddComponent<SeekingForJade.Economy.JadeTraderNPC>();
            SerializedObject soNpc = new SerializedObject(npc);
            soNpc.FindProperty("scaleZone").objectReferenceValue = scale.transform;
            soNpc.FindProperty("purchaseSpawnPoint").objectReferenceValue = buySpawn.transform;

            SerializedProperty bouldersProp = soNpc.FindProperty("availableBoulders");
            bouldersProp.arraySize = 1;
            bouldersProp.GetArrayElementAtIndex(0).objectReferenceValue = rockData;

            SerializedProperty pricesProp = soNpc.FindProperty("boulderPrices");
            pricesProp.arraySize = 1;
            pricesProp.GetArrayElementAtIndex(0).intValue = 50;

            soNpc.ApplyModifiedProperties();
        }

        private static void SpawnTestRocks()
        {
            // Remove previous test rocks
            GameObject existingGroup = GameObject.Find("TestRocks");
            if (existingGroup != null) Object.DestroyImmediate(existingGroup);

            GameObject rockGroup = new GameObject("TestRocks");

            RockData rockData = AssetDatabase.LoadAssetAtPath<RockData>("Assets/Settings/HpakantRiverBoulder.asset");

            // Rock 1 (On table left - resting stably horizontally)
            GameObject r1 = ProceduralRockGenerator.CreateRockGameObject(rockData, 1024, new Vector3(-0.55f, 1.15f, 3.5f));
            r1.transform.SetParent(rockGroup.transform);
            r1.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            r1.name = "Boulder_AppleGreen_1024";

            // Rock 2 (Sliced boulder on table right showing solid internal jade face)
            GameObject r2 = ProceduralRockGenerator.CreateRockGameObject(rockData, 55555, new Vector3(0.55f, 1.15f, 3.5f));
            r2.name = "Boulder_Imperial_55555";

            Material jadeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Jade_Internal.mat");
            var sliceResult = MeshSlicer.Slice(r2, r2.transform.position, Vector3.right, jadeMat);
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

                // Position and angle both halves so the solid internal jade faces face directly forward at player
                halfA.transform.position = new Vector3(0.85f, 1.10f, 3.5f);
                halfA.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                halfB.transform.position = new Vector3(0.52f, 1.10f, 3.5f);
                halfB.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            else
            {
                r2.transform.SetParent(rockGroup.transform);
            }

            // Rock 3 (On floor near bench)
            GameObject r3 = ProceduralRockGenerator.CreateRockGameObject(rockData, 99911, new Vector3(-1.4f, 0.35f, 2.5f));
            r3.transform.SetParent(rockGroup.transform);
            r3.transform.rotation = Quaternion.Euler(0f, 75f, 0f);
            r3.name = "Boulder_Mystery_99911";
        }
    }
}
#endif
