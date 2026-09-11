using System.Collections.Generic;
using UnityEngine;

namespace SeekingForJade.Environment
{
    public static class LowPolyMeshGenerator
    {
        /// <summary>
        /// Generates a flat-shaded stylized conifer/pine tree with 2 submeshes:
        /// Submesh 0: Trunk (Wood)
        /// Submesh 1: Foliage (Evergreen)
        /// </summary>
        public static Mesh GeneratePineTree(int seed, float height = 4.8f, float baseFoliageRadius = 1.6f)
        {
            var rng = new System.Random(seed);
            Mesh mesh = new Mesh { name = $"LowPolyPine_{seed}" };

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<int> trunkTriangles = new List<int>();
            List<int> foliageTriangles = new List<int>();

            // --- 1. Faceted Trunk ---
            int trunkSides = 6;
            float trunkHeight = height * 0.35f;
            float trunkBaseRadius = 0.22f;
            float trunkTopRadius = 0.14f;

            for (int i = 0; i < trunkSides; i++)
            {
                float angleA = i * Mathf.PI * 2f / trunkSides;
                float angleB = (i + 1) * Mathf.PI * 2f / trunkSides;

                Vector3 b0 = new Vector3(Mathf.Cos(angleA) * trunkBaseRadius, 0f, Mathf.Sin(angleA) * trunkBaseRadius);
                Vector3 b1 = new Vector3(Mathf.Cos(angleB) * trunkBaseRadius, 0f, Mathf.Sin(angleB) * trunkBaseRadius);
                Vector3 t0 = new Vector3(Mathf.Cos(angleA) * trunkTopRadius, trunkHeight, Mathf.Sin(angleA) * trunkTopRadius);
                Vector3 t1 = new Vector3(Mathf.Cos(angleB) * trunkTopRadius, trunkHeight, Mathf.Sin(angleB) * trunkTopRadius);

                AddFlatQuad(vertices, normals, uvs, trunkTriangles, b0, b1, t1, t0);
            }

            // --- 2. Foliage Tiers (3 to 4 conical tiers) ---
            int tiers = 4;
            int foliageSides = 7;
            float foliageStartHeight = height * 0.22f;
            float tierHeight = (height - foliageStartHeight) / tiers;

            for (int t = 0; t < tiers; t++)
            {
                float tNorm = (float)t / tiers;
                float bottomY = foliageStartHeight + t * tierHeight * 0.85f;
                float topY = bottomY + tierHeight * 1.35f;
                if (t == tiers - 1) topY = height; // Apex

                float tierRadius = Mathf.Lerp(baseFoliageRadius, 0.45f, tNorm);
                float tierRotation = (float)rng.NextDouble() * Mathf.PI * 2f;

                Vector3 apex = new Vector3(0f, topY, 0f);
                Vector3 baseCenter = new Vector3(0f, bottomY - 0.15f, 0f);

                for (int s = 0; s < foliageSides; s++)
                {
                    float a0 = tierRotation + s * Mathf.PI * 2f / foliageSides;
                    float a1 = tierRotation + (s + 1) * Mathf.PI * 2f / foliageSides;

                    // Slight random jitter for organic silhouette
                    float r0 = tierRadius * (0.92f + (float)rng.NextDouble() * 0.16f);
                    float r1 = tierRadius * (0.92f + (float)rng.NextDouble() * 0.16f);
                    float yJitter0 = ((float)rng.NextDouble() - 0.5f) * 0.08f;
                    float yJitter1 = ((float)rng.NextDouble() - 0.5f) * 0.08f;

                    Vector3 p0 = new Vector3(Mathf.Cos(a0) * r0, bottomY + yJitter0, Mathf.Sin(a0) * r0);
                    Vector3 p1 = new Vector3(Mathf.Cos(a1) * r1, bottomY + yJitter1, Mathf.Sin(a1) * r1);

                    // Outer cone face
                    AddFlatTriangle(vertices, normals, uvs, foliageTriangles, p0, p1, apex);
                    // Underside skirt face
                    AddFlatTriangle(vertices, normals, uvs, foliageTriangles, p1, p0, baseCenter);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trunkTriangles, 0);
            mesh.SetTriangles(foliageTriangles, 1);
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generates a flat-shaded stylized deciduous tree with trunk and puffy foliage clusters.
        /// </summary>
        public static Mesh GenerateDeciduousTree(int seed, float height = 4.2f)
        {
            var rng = new System.Random(seed);
            Mesh mesh = new Mesh { name = $"LowPolyDeciduous_{seed}" };

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<int> trunkTriangles = new List<int>();
            List<int> foliageTriangles = new List<int>();

            // Trunk: 6-sided faceted column with gentle tilt
            int sides = 6;
            float trunkHeight = height * 0.55f;
            float baseR = 0.25f;
            float topR = 0.16f;

            Vector3 trunkOffset = new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 0.35f,
                trunkHeight,
                ((float)rng.NextDouble() - 0.5f) * 0.35f
            );

            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides;
                float a1 = (i + 1) * Mathf.PI * 2f / sides;

                Vector3 b0 = new Vector3(Mathf.Cos(a0) * baseR, 0f, Mathf.Sin(a0) * baseR);
                Vector3 b1 = new Vector3(Mathf.Cos(a1) * baseR, 0f, Mathf.Sin(a1) * baseR);
                Vector3 t0 = trunkOffset + new Vector3(Mathf.Cos(a0) * topR, 0f, Mathf.Sin(a0) * topR);
                Vector3 t1 = trunkOffset + new Vector3(Mathf.Cos(a1) * topR, 0f, Mathf.Sin(a1) * topR);

                AddFlatQuad(vertices, normals, uvs, trunkTriangles, b0, b1, t1, t0);
            }

            // Foliage puffs: 3 to 4 faceted low-poly spheres positioned around crown
            Vector3[] puffCenters = new Vector3[]
            {
                trunkOffset + new Vector3(0f, 0.9f, 0f),
                trunkOffset + new Vector3(-0.65f, 0.4f, 0.3f),
                trunkOffset + new Vector3(0.65f, 0.3f, -0.4f),
                trunkOffset + new Vector3(0.2f, 0.5f, 0.6f)
            };
            float[] puffRadii = new float[] { 1.25f, 0.95f, 1.0f, 0.85f };

            for (int p = 0; p < puffCenters.Length; p++)
            {
                AddFacetedSphere(vertices, normals, uvs, foliageTriangles, puffCenters[p], puffRadii[p], rng);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trunkTriangles, 0);
            mesh.SetTriangles(foliageTriangles, 1);
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generates a flat-shaded low-poly boulder with organic faceting.
        /// </summary>
        public static Mesh GenerateLowPolyBoulder(int seed, float radius = 0.6f)
        {
            var rng = new System.Random(seed);
            Mesh mesh = new Mesh { name = $"LowPolyBoulder_{seed}" };

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            Vector3 center = Vector3.zero;
            AddFacetedSphere(vertices, normals, uvs, triangles, center, radius, rng, true, 1);

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generates an organic clustered bedrock outcrop for the mining quarry.
        /// Combines overlapping watertight low-poly rock mounds into a solid, rugged quarry pile.
        /// </summary>
        public static Mesh GenerateQuarryRockOutcrop(int seed)
        {
            var rng = new System.Random(seed);
            Mesh mesh = new Mesh { name = $"QuarryRockOutcrop_{seed}" };

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            // Cluster of 4 overlapping mounds forming a rugged quarry pit/pile
            Vector3[] moundCenters = new Vector3[]
            {
                new Vector3(0f, 0.25f, 0f),
                new Vector3(-0.55f, 0.15f, 0.45f),
                new Vector3(0.60f, 0.12f, -0.35f),
                new Vector3(-0.25f, 0.10f, -0.60f)
            };
            float[] moundRadii = new float[] { 1.25f, 0.95f, 1.05f, 0.85f };

            for (int m = 0; m < moundCenters.Length; m++)
            {
                AddFacetedSphere(vertices, normals, uvs, triangles, moundCenters[m], moundRadii[m], rng, true, 1);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generates an expansive faceted terrain mesh with perimeter rolling hills and a level central clearing.
        /// </summary>
        public static Mesh GenerateFacetedTerrain(int gridSize = 32, float cellSize = 1.4f)
        {
            Mesh mesh = new Mesh { name = "LowPolyClearingTerrain" };

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            float halfSize = (gridSize * cellSize) * 0.5f;
            Vector3[,] grid = new Vector3[gridSize + 1, gridSize + 1];

            for (int x = 0; x <= gridSize; x++)
            {
                for (int z = 0; z <= gridSize; z++)
                {
                    float worldX = x * cellSize - halfSize;
                    float worldZ = z * cellSize - halfSize;

                    // Distance from central workshop clearing (around z = 3.0)
                    float distFromCenter = Mathf.Sqrt(worldX * worldX + (worldZ - 3.0f) * (worldZ - 3.0f));

                    float height = 0f;
                    if (distFromCenter > 7.5f)
                    {
                        float rimFactor = Mathf.Clamp01((distFromCenter - 7.5f) / 12f);
                        float perlin = Mathf.PerlinNoise(worldX * 0.12f + 10f, worldZ * 0.12f + 10f);
                        height = Mathf.Pow(rimFactor, 1.8f) * (2.8f + perlin * 2.2f);
                    }
                    else
                    {
                        // Perfectly flat workshop clearing
                        height = 0f;
                    }

                    grid[x, z] = new Vector3(worldX, height, worldZ);
                }
            }

            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 v00 = grid[x, z];
                    Vector3 v10 = grid[x + 1, z];
                    Vector3 v01 = grid[x, z + 1];
                    Vector3 v11 = grid[x + 1, z + 1];

                    // Triangulate into 2 flat quads
                    AddFlatTriangle(vertices, normals, uvs, triangles, v00, v01, v11);
                    AddFlatTriangle(vertices, normals, uvs, triangles, v00, v11, v10);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generates a stylized winding dirt path ribbon mesh.
        /// </summary>
        public static Mesh GeneratePathRibbon(Vector3[] waypoints, float width = 1.35f)
        {
            Mesh mesh = new Mesh { name = "LowPolyDirtPath" };
            if (waypoints == null || waypoints.Length < 2) return mesh;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Vector3 p0 = waypoints[i];
                Vector3 p1 = waypoints[i + 1];

                Vector3 forward = (p1 - p0).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                float halfW = width * 0.5f;

                Vector3 l0 = p0 - right * halfW + Vector3.up * 0.04f;
                Vector3 r0 = p0 + right * halfW + Vector3.up * 0.04f;
                Vector3 l1 = p1 - right * halfW + Vector3.up * 0.04f;
                Vector3 r1 = p1 + right * halfW + Vector3.up * 0.04f;

                AddFlatQuad(vertices, normals, uvs, triangles, l0, r0, r1, l1);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

        // --- Helper Geometry Builders ---

        private static void AddFlatTriangle(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            int baseIdx = verts.Count;

            verts.Add(a);
            verts.Add(b);
            verts.Add(c);

            norms.Add(normal);
            norms.Add(normal);
            norms.Add(normal);

            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(0.5f, 1f));

            tris.Add(baseIdx);
            tris.Add(baseIdx + 1);
            tris.Add(baseIdx + 2);
        }

        private static void AddFlatQuad(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            AddFlatTriangle(verts, norms, uvs, tris, v0, v1, v2);
            AddFlatTriangle(verts, norms, uvs, tris, v0, v2, v3);
        }

        private static void AddFacetedSphere(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 center, float radius, System.Random rng, bool isBoulder = false, int subdivisions = 1)
        {
            float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

            List<Vector3> icoVerts = new List<Vector3>
            {
                new Vector3(-1,  t,  0).normalized,
                new Vector3( 1,  t,  0).normalized,
                new Vector3(-1, -t,  0).normalized,
                new Vector3( 1, -t,  0).normalized,

                new Vector3( 0, -1,  t).normalized,
                new Vector3( 0,  1,  t).normalized,
                new Vector3( 0, -1, -t).normalized,
                new Vector3( 0,  1, -t).normalized,

                new Vector3( t,  0, -1).normalized,
                new Vector3( t,  0,  1).normalized,
                new Vector3(-t,  0, -1).normalized,
                new Vector3(-t,  0,  1).normalized
            };

            List<int> icoTris = new List<int>
            {
                0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
                1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
                3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
                4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
            };

            // Subdivide if requested for smoother organic faceting
            for (int sub = 0; sub < subdivisions; sub++)
            {
                List<int> newTris = new List<int>();
                Dictionary<long, int> midPointCache = new Dictionary<long, int>();

                for (int i = 0; i < icoTris.Count; i += 3)
                {
                    int a = icoTris[i];
                    int b = icoTris[i + 1];
                    int c = icoTris[i + 2];

                    int ab = GetMidPointIndex(midPointCache, icoVerts, a, b);
                    int bc = GetMidPointIndex(midPointCache, icoVerts, b, c);
                    int ca = GetMidPointIndex(midPointCache, icoVerts, c, a);

                    newTris.AddRange(new int[] { a, ab, ca });
                    newTris.AddRange(new int[] { b, bc, ab });
                    newTris.AddRange(new int[] { c, ca, bc });
                    newTris.AddRange(new int[] { ab, bc, ca });
                }

                icoTris = newTris;
            }

            // Aspect ratio deformation for organic silhouettes
            Vector3 scale = isBoulder
                ? new Vector3(
                    Mathf.Lerp(0.85f, 1.25f, (float)rng.NextDouble()),
                    Mathf.Lerp(0.70f, 1.05f, (float)rng.NextDouble()),
                    Mathf.Lerp(0.85f, 1.30f, (float)rng.NextDouble()))
                : Vector3.one;

            // Pre-deform ALL shared vertices ONCE so edges never pull apart!
            Vector3[] finalVerts = new Vector3[icoVerts.Count];
            float noiseOffset = (float)rng.NextDouble() * 50f;
            for (int v = 0; v < icoVerts.Count; v++)
            {
                Vector3 baseDir = icoVerts[v];
                float perlin = Mathf.PerlinNoise(baseDir.x * 2f + noiseOffset, baseDir.y * 2f + noiseOffset) * 2f - 1f;
                float jitter = 1.0f + perlin * (isBoulder ? 0.18f : 0.08f);

                Vector3 p = baseDir * (radius * jitter);
                p.x *= scale.x;
                p.y *= scale.y;
                p.z *= scale.z;

                finalVerts[v] = center + p;
            }

            // Generate flat-shaded triangles (unshared face vertices for crisp low-poly look)
            for (int i = 0; i < icoTris.Count; i += 3)
            {
                Vector3 pA = finalVerts[icoTris[i]];
                Vector3 pB = finalVerts[icoTris[i + 1]];
                Vector3 pC = finalVerts[icoTris[i + 2]];

                AddFlatTriangle(verts, norms, uvs, tris, pA, pB, pC);
            }
        }

        private static int GetMidPointIndex(Dictionary<long, int> cache, List<Vector3> verts, int i1, int i2)
        {
            long smaller = Mathf.Min(i1, i2);
            long greater = Mathf.Max(i1, i2);
            long key = (smaller << 32) + greater;

            if (cache.TryGetValue(key, out int ret)) return ret;

            Vector3 p1 = verts[i1];
            Vector3 p2 = verts[i2];
            Vector3 middle = ((p1 + p2) * 0.5f).normalized;

            verts.Add(middle);
            int index = verts.Count - 1;
            cache.Add(key, index);
            return index;
        }
    }
}
