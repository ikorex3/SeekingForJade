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

            Mesh rockMesh = GenerateRockMesh(seed);
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

        public static Mesh GenerateRockMesh(int seed, int subdivisions = 2, float baseRadius = 0.35f)
        {
            var rng = new System.Random(seed);
            float noiseScale = 2.5f;
            float noiseStrength = 0.12f;

            // Random non-uniform scale factors to give unique rock silhouettes
            Vector3 aspectScale = new Vector3(
                Mathf.Lerp(0.7f, 1.3f, (float)rng.NextDouble()),
                Mathf.Lerp(0.6f, 1.1f, (float)rng.NextDouble()),
                Mathf.Lerp(0.8f, 1.4f, (float)rng.NextDouble())
            );

            // Start with an icosphere base
            Mesh baseSphere = CreateIcoSphere(subdivisions, baseRadius);
            Vector3[] vertices = baseSphere.vertices;
            Vector3[] normals = baseSphere.normals;
            Vector2[] uvs = new Vector2[vertices.Length];

            float seedOffset = (float)(rng.NextDouble() * 100.0);

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                // Apply 3D Perlin noise displacement
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
