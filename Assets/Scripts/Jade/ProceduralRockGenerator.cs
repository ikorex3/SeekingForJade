using UnityEngine;

namespace SeekingForJade.Jade
{
    public static class ProceduralRockGenerator
    {
        public static GameObject CreateRockGameObject(RockData rockData, int seed, Vector3 position)
        {
            GameObject rockObj = new GameObject($"Rock_{seed}");
            rockObj.transform.position = position;

            MeshFilter filter = rockObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = rockObj.AddComponent<MeshRenderer>();

            Mesh rockMesh = null;
#if UNITY_EDITOR
            if (seed % 2 == 0)
            {
                Mesh template = GetRockPackTemplateMesh(seed);
                if (template != null)
                {
                    rockMesh = Object.Instantiate(template);
                    Vector3 bSize = template.bounds.size;
                    float maxDim = Mathf.Max(bSize.x, bSize.y, bSize.z);
                    if (maxDim > 0.001f)
                    {
                        float scale = 0.65f / maxDim;
                        Vector3[] v = rockMesh.vertices;
                        for (int i = 0; i < v.Length; i++) v[i] *= scale;
                        rockMesh.vertices = v;
                        rockMesh.RecalculateBounds();
                        rockMesh.RecalculateNormals();
                    }
                }
            }
#endif
            if (rockMesh == null)
            {
                rockMesh = GenerateRockMesh(seed);
            }

            filter.sharedMesh = rockMesh;

            Material crustMat = rockData != null && rockData.crustMaterial != null 
                ? rockData.crustMaterial 
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));

            renderer.sharedMaterial = crustMat;

            MeshCollider collider = rockObj.AddComponent<MeshCollider>();
            collider.sharedMesh = rockMesh;
            collider.convex = true;

            Rigidbody rb = rockObj.AddComponent<Rigidbody>();
            rb.mass = 8.0f;
            rb.linearDamping = 0.8f;
            rb.angularDamping = 2.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            ProceduralRock rockComponent = rockObj.AddComponent<ProceduralRock>();
            rockComponent.Initialize(rockData, seed);

            RockImpactBreaker breaker = rockObj.AddComponent<RockImpactBreaker>();
#if UNITY_EDITOR
            breaker.JadeCapMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Jade_Internal.mat");
#endif

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

        public static Mesh GenerateRockMesh(int seed, int subdivisions = 3, float baseRadius = 0.35f, bool flatShaded = true)
        {
            var rng = new System.Random(seed);
            float noiseScale = 2.8f;
            float noiseStrength = 0.14f;

            // Random non-uniform scale factors to give unique rock silhouettes
            Vector3 aspectScale = new Vector3(
                Mathf.Lerp(0.75f, 1.25f, (float)rng.NextDouble()),
                Mathf.Lerp(0.65f, 1.10f, (float)rng.NextDouble()),
                Mathf.Lerp(0.75f, 1.35f, (float)rng.NextDouble())
            );

            // Start with an icosphere base (subdivisions=3 yields 320 triangles)
            Mesh baseSphere = CreateIcoSphere(subdivisions, baseRadius);
            Vector3[] vertices = baseSphere.vertices;
            Vector3[] normals = baseSphere.normals;
            Vector2[] uvs = new Vector2[vertices.Length];

            float seedOffset = (float)(rng.NextDouble() * 100.0);

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                // Multi-octave Perlin noise displacement
                float n = Mathf.PerlinNoise(v.x * noiseScale + seedOffset, v.y * noiseScale + seedOffset) * 2f - 1f;
                float n2 = Mathf.PerlinNoise(v.y * noiseScale + seedOffset + 31f, v.z * noiseScale + seedOffset + 17f) * 2f - 1f;

                float displacement = (n * 0.7f + n2 * 0.3f) * noiseStrength;
                Vector3 displaced = (v + normals[i] * displacement);

                // Apply aspect scale
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
