using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace SeekingForJade.Jade
{
    public static class ProceduralRockGenerator
    {
        private static GameObject rockNetworkPrefab;
        private static Dictionary<string, RockData> rockDataCache;

        public static RockData GetRockDataByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (rockDataCache == null)
            {
                rockDataCache = new Dictionary<string, RockData>();
#if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:RockData");
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    RockData data = UnityEditor.AssetDatabase.LoadAssetAtPath<RockData>(path);
                    if (data != null && !rockDataCache.ContainsKey(data.name))
                    {
                        rockDataCache.Add(data.name, data);
                    }
                }
#else
                RockData[] loaded = Resources.LoadAll<RockData>("");
                foreach (var data in loaded)
                {
                    if (data != null && !rockDataCache.ContainsKey(data.name))
                    {
                        rockDataCache.Add(data.name, data);
                    }
                }
#endif
            }

            if (rockDataCache.TryGetValue(name, out RockData result))
            {
                return result;
            }
            return null;
        }

        public static GameObject GetRockNetworkPrefab()
        {
            if (rockNetworkPrefab == null)
            {
#if UNITY_EDITOR
                rockNetworkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProceduralRockNetwork.prefab");
#else
                rockNetworkPrefab = Resources.Load<GameObject>("ProceduralRockNetwork");
#endif
            }
            return rockNetworkPrefab;
        }

        public static GameObject CreateRockGameObject(RockData rockData, int seed, Vector3 position)
        {
            GameObject rockObj;

            if (rockNetworkPrefab == null)
            {
                GetRockNetworkPrefab();
            }

            if (rockNetworkPrefab != null)
            {
                rockObj = Object.Instantiate(rockNetworkPrefab, position, Quaternion.identity);
                rockObj.name = $"Rock_{seed}";
            }
            else
            {
                rockObj = new GameObject($"Rock_{seed}");
                rockObj.transform.position = position;
            }

            MeshFilter filter = rockObj.GetComponent<MeshFilter>();
            if (filter == null) filter = rockObj.AddComponent<MeshFilter>();

            MeshRenderer renderer = rockObj.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = rockObj.AddComponent<MeshRenderer>();

            Mesh rockMesh = GenerateMeshForSeed(seed);
            filter.sharedMesh = rockMesh;

            Material crustMat = rockData != null && rockData.crustMaterial != null 
                ? rockData.crustMaterial 
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));

            renderer.sharedMaterial = crustMat;

            MeshCollider collider = rockObj.GetComponent<MeshCollider>();
            if (collider == null) collider = rockObj.AddComponent<MeshCollider>();
            collider.sharedMesh = rockMesh;
            collider.convex = true;

            Rigidbody rb = rockObj.GetComponent<Rigidbody>();
            if (rb == null) rb = rockObj.AddComponent<Rigidbody>();
            rb.mass = 8.0f;
            rb.linearDamping = 0.8f;
            rb.angularDamping = 2.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            NetworkObject netObj = rockObj.GetComponent<NetworkObject>();
            if (netObj == null) netObj = rockObj.AddComponent<NetworkObject>();

            NetworkTransform netTransform = rockObj.GetComponent<NetworkTransform>();
            if (netTransform == null) netTransform = rockObj.AddComponent<NetworkTransform>();
            netTransform.InLocalSpace = false;
            netTransform.Interpolate = true;

            ProceduralRock rockComponent = rockObj.GetComponent<ProceduralRock>();
            if (rockComponent == null) rockComponent = rockObj.AddComponent<ProceduralRock>();
            rockComponent.Initialize(rockData, seed);

            RockImpactBreaker breaker = rockObj.GetComponent<RockImpactBreaker>();
            if (breaker == null) breaker = rockObj.AddComponent<RockImpactBreaker>();
#if UNITY_EDITOR
            breaker.JadeCapMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Jade_Internal.mat");
#else
            breaker.JadeCapMaterial = Resources.Load<Material>("M_Jade_Internal");
#endif

            if (Application.isPlaying && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer && !netObj.IsSpawned)
            {
                netObj.Spawn();
                rockComponent.PushLocalStateToNetwork();
            }

            return rockObj;
        }

#if UNITY_EDITOR
        private static Mesh[] cachedRockPackMeshes;
        public static Mesh GetRockPackTemplateMesh(int seed)
        {
            if (cachedRockPackMeshes == null || cachedRockPackMeshes.Length == 0)
            {
                var list = new System.Collections.Generic.List<Mesh>();
                for (int t = 1; t <= 6; t++)
                {
                    for (int v = 1; v <= 4; v++)
                    {
                        string pPath = $"Assets/BrokenVector/LowPolyRockPack/Prefabs/Rock Type{t} 0{v}.prefab";
                        GameObject p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
                        if (p != null)
                        {
                            MeshFilter mf = p.GetComponent<MeshFilter>();
                            if (mf != null && mf.sharedMesh != null)
                            {
                                list.Add(mf.sharedMesh);
                            }
                        }
                    }
                }
                cachedRockPackMeshes = list.ToArray();
            }

            if (cachedRockPackMeshes != null && cachedRockPackMeshes.Length > 0)
            {
                int idx = Mathf.Abs(seed) % cachedRockPackMeshes.Length;
                return cachedRockPackMeshes[idx];
            }
            return null;
        }
#endif

        public static Mesh GenerateMeshForSeed(int seed)
        {
            return GenerateRockMesh(seed);
        }

        public static Mesh GenerateRockMesh(int seed, int subdivisions = 3, float baseRadius = 0.35f, bool flatShaded = true)
        {
            // Use fixed seed for standard raw rock shape across all clients
            var rng = new System.Random(123456);

            float noiseScale = 2.4f;
            float noiseStrength = 0.16f;
            Vector3 aspectScale = new Vector3(1.10f, 0.88f, 1.05f);

            // Start with an icosphere base (subdivisions=3 yields 320 triangles)
            Mesh baseSphere = CreateIcoSphere(subdivisions, baseRadius);
            Vector3[] vertices = baseSphere.vertices;
            Vector3[] normals = baseSphere.normals;
            Vector2[] uvs = new Vector2[vertices.Length];

            float seedOffset = 42.5f;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                // Multi-octave Perlin noise displacement
                float n = Mathf.PerlinNoise(v.x * noiseScale + seedOffset, v.y * noiseScale + seedOffset) * 2f - 1f;
                float n2 = Mathf.PerlinNoise(v.y * noiseScale + seedOffset + 31f, v.z * noiseScale + seedOffset + 17f) * 2f - 1f;

                float displacement = (n * 0.65f + n2 * 0.35f) * noiseStrength;
                Vector3 displaced = (v + normals[i] * displacement);

                // Apply archetype aspect scale
                displaced.x *= aspectScale.x;
                displaced.y *= aspectScale.y;
                displaced.z *= aspectScale.z;

                vertices[i] = displaced;

                // Triplanar spherical UV mapping
                Vector3 normalized = displaced.normalized;
                uvs[i] = new Vector2(
                    Mathf.Atan2(normalized.z, normalized.x) / (2f * Mathf.PI) + 0.5f,
                    Mathf.Asin(normalized.y) / Mathf.PI + 0.5f
                );
            }

            baseSphere.vertices = vertices;
            baseSphere.uv = uvs;
            baseSphere.RecalculateNormals();
            baseSphere.RecalculateBounds();

            if (flatShaded)
            {
                // Convert to flat-shaded (unshared vertices per triangle for crisp low-poly facets)
                int[] oldTris = baseSphere.triangles;
                Vector3[] flatVerts = new Vector3[oldTris.Length];
                Vector2[] flatUvs = new Vector2[oldTris.Length];
                int[] flatTris = new int[oldTris.Length];

                for (int t = 0; t < oldTris.Length; t++)
                {
                    int idx = oldTris[t];
                    flatVerts[t] = vertices[idx];
                    flatUvs[t] = uvs[idx];
                    flatTris[t] = t;
                }

                Mesh flatMesh = new Mesh();
                flatMesh.name = $"ProceduralRockMesh_{seed}";
                flatMesh.vertices = flatVerts;
                flatMesh.triangles = flatTris;
                flatMesh.uv = flatUvs;
                flatMesh.RecalculateNormals();
                flatMesh.RecalculateBounds();
                return flatMesh;
            }

            baseSphere.name = $"ProceduralRockMesh_{seed}";
            return baseSphere;
        }

        private static Mesh CreateIcoSphere(int recursions, float radius)
        {
            Mesh mesh = new Mesh();

            float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

            var vertList = new System.Collections.Generic.List<Vector3>
            {
                new Vector3(-1,  t,  0).normalized * radius,
                new Vector3( 1,  t,  0).normalized * radius,
                new Vector3(-1, -t,  0).normalized * radius,
                new Vector3( 1, -t,  0).normalized * radius,
                new Vector3( 0, -1,  t).normalized * radius,
                new Vector3( 0,  1,  t).normalized * radius,
                new Vector3( 0, -1, -t).normalized * radius,
                new Vector3( 0,  1, -t).normalized * radius,
                new Vector3( t,  0, -1).normalized * radius,
                new Vector3( t,  0,  1).normalized * radius,
                new Vector3(-t,  0, -1).normalized * radius,
                new Vector3(-t,  0,  1).normalized * radius
            };

            var faceList = new System.Collections.Generic.List<int>
            {
                0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
                1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
                3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
                4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
            };

            mesh.SetVertices(vertList);
            mesh.SetTriangles(faceList, 0);
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
