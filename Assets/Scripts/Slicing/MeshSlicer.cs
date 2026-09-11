using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeekingForJade.Slicing
{
    public static class MeshSlicer
    {
        private class SlicedSide
        {
            public List<Vector3> vertices = new List<Vector3>();
            public List<Vector3> normals = new List<Vector3>();
            public List<Vector2> uvs = new List<Vector2>();
            public List<List<int>> submeshTriangles = new List<List<int>>();
            public List<int> capTriangles = new List<int>();
            public List<Vector3> capEdgePointsA = new List<Vector3>();
            public List<Vector3> capEdgePointsB = new List<Vector3>();

            public void InitializeSubmeshes(int count)
            {
                submeshTriangles.Clear();
                for (int i = 0; i < count; i++)
                {
                    submeshTriangles.Add(new List<int>());
                }
            }

            public int AddVertex(Vector3 pos, Vector3 norm, Vector2 uv)
            {
                int index = vertices.Count;
                vertices.Add(pos);
                normals.Add(norm);
                uvs.Add(uv);
                return index;
            }

            public void AddTriangle(int submeshIndex, int i0, int i1, int i2)
            {
                submeshTriangles[submeshIndex].Add(i0);
                submeshTriangles[submeshIndex].Add(i1);
                submeshTriangles[submeshIndex].Add(i2);
            }
        }

        public struct SlicedResult
        {
            public GameObject positiveSideObject;
            public GameObject negativeSideObject;
            public bool success;
        }

        public static SlicedResult Slice(GameObject target, Vector3 planePoint, Vector3 planeNormal, Material capMaterial)
        {
            SlicedResult result = new SlicedResult { success = false };

            if (target == null) return result;
            MeshFilter filter = target.GetComponent<MeshFilter>();
            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (filter == null || renderer == null || filter.sharedMesh == null) return result;

            Mesh originalMesh = filter.sharedMesh;
            Transform targetTransform = target.transform;

            // Convert world plane to local space
            Vector3 localPlanePoint = targetTransform.InverseTransformPoint(planePoint);
            Vector3 localPlaneNormal = targetTransform.InverseTransformDirection(planeNormal).normalized;

            Vector3[] origVerts = originalMesh.vertices;
            Vector3[] origNorms = originalMesh.normals.Length == origVerts.Length ? originalMesh.normals : new Vector3[origVerts.Length];
            Vector2[] origUvs = originalMesh.uv.Length == origVerts.Length ? originalMesh.uv : new Vector2[origVerts.Length];
            int submeshCount = originalMesh.subMeshCount;

            // Calculate distance to plane for all vertices
            float[] distances = new float[origVerts.Length];
            int positiveCount = 0;
            int negativeCount = 0;

            for (int i = 0; i < origVerts.Length; i++)
            {
                distances[i] = Vector3.Dot(localPlaneNormal, origVerts[i] - localPlanePoint);
                if (distances[i] > 0.0001f) positiveCount++;
                else if (distances[i] < -0.0001f) negativeCount++;
            }

            // If the plane does not intersect the mesh bounds
            if (positiveCount == 0 || negativeCount == 0)
            {
                return result;
            }

            SlicedSide sidePositive = new SlicedSide();
            SlicedSide sideNegative = new SlicedSide();
            sidePositive.InitializeSubmeshes(submeshCount);
            sideNegative.InitializeSubmeshes(submeshCount);

            // Process triangles per submesh
            for (int sub = 0; sub < submeshCount; sub++)
            {
                int[] triangles = originalMesh.GetTriangles(sub);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int i0 = triangles[i];
                    int i1 = triangles[i + 1];
                    int i2 = triangles[i + 2];

                    float d0 = distances[i0];
                    float d1 = distances[i1];
                    float d2 = distances[i2];

                    bool side0 = d0 > 0f;
                    bool side1 = d1 > 0f;
                    bool side2 = d2 > 0f;

                    if (side0 == side1 && side1 == side2)
                    {
                        // All 3 vertices on the same side
                        SlicedSide targetSide = side0 ? sidePositive : sideNegative;
                        int n0 = targetSide.AddVertex(origVerts[i0], origNorms[i0], origUvs[i0]);
                        int n1 = targetSide.AddVertex(origVerts[i1], origNorms[i1], origUvs[i1]);
                        int n2 = targetSide.AddVertex(origVerts[i2], origNorms[i2], origUvs[i2]);
                        targetSide.AddTriangle(sub, n0, n1, n2);
                    }
                    else
                    {
                        // Triangle is cut by plane
                        SplitTriangle(
                            sub,
                            i0, i1, i2,
                            origVerts, origNorms, origUvs, distances,
                            localPlanePoint, localPlaneNormal,
                            sidePositive, sideNegative
                        );
                    }
                }
            }

            // Generate caps for both sides
            GenerateCap(sidePositive, localPlanePoint, localPlaneNormal, true);
            GenerateCap(sideNegative, localPlanePoint, localPlaneNormal, false);

            // Build meshes
            Mesh meshPositive = CreateMeshFromSide(sidePositive, originalMesh.name + "_SliceA");
            Mesh meshNegative = CreateMeshFromSide(sideNegative, originalMesh.name + "_SliceB");

            // Setup materials array (original + cap material)
            Material[] origMaterials = renderer.sharedMaterials;
            Material effectiveCapMat = capMaterial;
            if (effectiveCapMat == null)
            {
#if UNITY_EDITOR
                effectiveCapMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Jade_Internal.mat");
#endif
                if (effectiveCapMat == null && origMaterials != null && origMaterials.Length > 0)
                {
                    effectiveCapMat = origMaterials[0];
                }
            }

            Material[] newMaterials;
            if (effectiveCapMat != null)
            {
                newMaterials = new Material[origMaterials.Length + 1];
                Array.Copy(origMaterials, newMaterials, origMaterials.Length);
                newMaterials[newMaterials.Length - 1] = effectiveCapMat;
            }
            else
            {
                newMaterials = origMaterials;
            }

            // Create GameObject A (reusing existing target)
            filter.sharedMesh = meshPositive;
            renderer.sharedMaterials = newMaterials;
            UpdateCollider(target, meshPositive);

            // Create GameObject B
            GameObject objectB = UnityEngine.Object.Instantiate(target, targetTransform.parent);
            objectB.name = target.name + "_SliceB";
            objectB.transform.position = targetTransform.position;
            objectB.transform.rotation = targetTransform.rotation;
            objectB.transform.localScale = targetTransform.localScale;

            MeshFilter filterB = objectB.GetComponent<MeshFilter>();
            MeshRenderer rendererB = objectB.GetComponent<MeshRenderer>();
            filterB.sharedMesh = meshNegative;
            rendererB.sharedMaterials = newMaterials;
            UpdateCollider(objectB, meshNegative);

            // Apply slight separation force if rigidbodies exist
            Rigidbody rbA = target.GetComponent<Rigidbody>();
            Rigidbody rbB = objectB.GetComponent<Rigidbody>();
            if (rbA != null && rbB != null)
            {
                rbA.linearVelocity = Vector3.zero;
                rbB.linearVelocity = Vector3.zero;
                rbA.AddForce(planeNormal * 0.5f, ForceMode.Impulse);
                rbB.AddForce(-planeNormal * 0.5f, ForceMode.Impulse);
            }

            result.positiveSideObject = target;
            result.negativeSideObject = objectB;
            result.success = true;
            return result;
        }

        private static void SplitTriangle(
            int submesh,
            int i0, int i1, int i2,
            Vector3[] verts, Vector3[] norms, Vector2[] uvs, float[] dists,
            Vector3 planePt, Vector3 planeNorm,
            SlicedSide sidePos, SlicedSide sideNeg)
        {
            // Reorder vertices so that v0 is alone on one side, and v1, v2 are on the other side
            int v0 = i0, v1 = i1, v2 = i2;
            bool s0 = dists[v0] > 0f;
            bool s1 = dists[v1] > 0f;
            bool s2 = dists[v2] > 0f;

            if (s0 == s1)
            {
                // v2 is lone vertex
                v0 = i2; v1 = i0; v2 = i1;
            }
            else if (s0 == s2)
            {
                // v1 is lone vertex
                v0 = i1; v1 = i2; v2 = i0;
            }

            bool loneIsPositive = dists[v0] > 0f;
            SlicedSide loneSide = loneIsPositive ? sidePos : sideNeg;
            SlicedSide otherSide = loneIsPositive ? sideNeg : sidePos;

            // Compute cut points along edge (v0 -> v1) and (v0 -> v2)
            float d0 = dists[v0];
            float d1 = dists[v1];
            float d2 = dists[v2];

            float t1 = d0 / (d0 - d1);
            float t2 = d0 / (d0 - d2);

            Vector3 cutPos1 = Vector3.Lerp(verts[v0], verts[v1], t1);
            Vector3 cutNorm1 = Vector3.Slerp(norms[v0], norms[v1], t1).normalized;
            Vector2 cutUv1 = Vector2.Lerp(uvs[v0], uvs[v1], t1);

            Vector3 cutPos2 = Vector3.Lerp(verts[v0], verts[v2], t2);
            Vector3 cutNorm2 = Vector3.Slerp(norms[v0], norms[v2], t2).normalized;
            Vector2 cutUv2 = Vector2.Lerp(uvs[v0], uvs[v2], t2);

            // Lone side gets 1 triangle (v0, cut1, cut2)
            int l_v0 = loneSide.AddVertex(verts[v0], norms[v0], uvs[v0]);
            int l_c1 = loneSide.AddVertex(cutPos1, cutNorm1, cutUv1);
            int l_c2 = loneSide.AddVertex(cutPos2, cutNorm2, cutUv2);
            loneSide.AddTriangle(submesh, l_v0, l_c1, l_c2);

            // Other side gets 2 triangles (quad): (v1, v2, cut2) and (v1, cut2, cut1)
            int o_v1 = otherSide.AddVertex(verts[v1], norms[v1], uvs[v1]);
            int o_v2 = otherSide.AddVertex(verts[v2], norms[v2], uvs[v2]);
            int o_c1 = otherSide.AddVertex(cutPos1, cutNorm1, cutUv1);
            int o_c2 = otherSide.AddVertex(cutPos2, cutNorm2, cutUv2);
            otherSide.AddTriangle(submesh, o_v1, o_v2, o_c2);
            otherSide.AddTriangle(submesh, o_v1, o_c2, o_c1);

            // Record edge for cap polygon
            if (loneIsPositive)
            {
                sidePos.capEdgePointsA.Add(cutPos1);
                sidePos.capEdgePointsB.Add(cutPos2);
                sideNeg.capEdgePointsA.Add(cutPos2);
                sideNeg.capEdgePointsB.Add(cutPos1);
            }
            else
            {
                sidePos.capEdgePointsA.Add(cutPos2);
                sidePos.capEdgePointsB.Add(cutPos1);
                sideNeg.capEdgePointsA.Add(cutPos1);
                sideNeg.capEdgePointsB.Add(cutPos2);
            }
        }

        private static void GenerateCap(SlicedSide side, Vector3 planePoint, Vector3 planeNormal, bool isPositive)
        {
            if (side.capEdgePointsA.Count < 3) return;

            Vector3 capNormal = (isPositive ? -planeNormal : planeNormal).normalized;

            // Planar UV axes
            Vector3 uAxis = Vector3.Cross(capNormal, Vector3.up).normalized;
            if (uAxis.sqrMagnitude < 0.001f)
            {
                uAxis = Vector3.Cross(capNormal, Vector3.right).normalized;
            }
            Vector3 vAxis = Vector3.Cross(capNormal, uAxis).normalized;

            // Collect all unique points from cut edges
            List<Vector3> uniquePoints = new List<Vector3>();
            for (int i = 0; i < side.capEdgePointsA.Count; i++)
            {
                AddUniquePoint(uniquePoints, side.capEdgePointsA[i]);
                AddUniquePoint(uniquePoints, side.capEdgePointsB[i]);
            }

            if (uniquePoints.Count < 3) return;

            // Calculate centroid
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < uniquePoints.Count; i++)
            {
                centroid += uniquePoints[i];
            }
            centroid /= uniquePoints.Count;

            // Sort points by angle around centroid on the cutting plane
            uniquePoints.Sort((a, b) =>
            {
                Vector3 da = a - centroid;
                Vector3 db = b - centroid;
                float angleA = Mathf.Atan2(Vector3.Dot(da, vAxis), Vector3.Dot(da, uAxis));
                float angleB = Mathf.Atan2(Vector3.Dot(db, vAxis), Vector3.Dot(db, uAxis));
                return angleA.CompareTo(angleB);
            });

            // Add centroid vertex
            Vector2 centroidUv = new Vector2(Vector3.Dot(centroid, uAxis), Vector3.Dot(centroid, vAxis)) * 2.0f;
            int cIndex = side.AddVertex(centroid, capNormal, centroidUv);

            // Add sorted perimeter vertices
            int[] vIndices = new int[uniquePoints.Count];
            for (int i = 0; i < uniquePoints.Count; i++)
            {
                Vector3 pt = uniquePoints[i];
                Vector2 uv = new Vector2(Vector3.Dot(pt, uAxis), Vector3.Dot(pt, vAxis)) * 2.0f;
                vIndices[i] = side.AddVertex(pt, capNormal, uv);
            }

            // Create fan triangles with verified outward normal
            int n = uniquePoints.Count;
            for (int i = 0; i < n; i++)
            {
                int iCurr = vIndices[i];
                int iNext = vIndices[(i + 1) % n];

                Vector3 edge1 = uniquePoints[i] - centroid;
                Vector3 edge2 = uniquePoints[(i + 1) % n] - centroid;
                Vector3 cross = Vector3.Cross(edge1, edge2);

                if (Vector3.Dot(cross, capNormal) >= 0f)
                {
                    side.capTriangles.Add(cIndex);
                    side.capTriangles.Add(iCurr);
                    side.capTriangles.Add(iNext);
                }
                else
                {
                    side.capTriangles.Add(cIndex);
                    side.capTriangles.Add(iNext);
                    side.capTriangles.Add(iCurr);
                }
            }
        }

        private static void AddUniquePoint(List<Vector3> list, Vector3 pt, float eps = 0.0005f)
        {
            float epsSq = eps * eps;
            for (int i = 0; i < list.Count; i++)
            {
                if ((list[i] - pt).sqrMagnitude < epsSq)
                    return;
            }
            list.Add(pt);
        }

        private static Mesh CreateMeshFromSide(SlicedSide side, string name)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(side.vertices);
            mesh.SetNormals(side.normals);
            mesh.SetUVs(0, side.uvs);

            bool hasCap = side.capTriangles.Count > 0;
            int totalSubmeshes = side.submeshTriangles.Count + (hasCap ? 1 : 0);
            mesh.subMeshCount = totalSubmeshes;

            for (int i = 0; i < side.submeshTriangles.Count; i++)
            {
                mesh.SetTriangles(side.submeshTriangles[i], i);
            }

            if (hasCap)
            {
                mesh.SetTriangles(side.capTriangles, totalSubmeshes - 1);
            }

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static void UpdateCollider(GameObject go, Mesh mesh)
        {
            MeshCollider collider = go.GetComponent<MeshCollider>();
            if (collider != null)
            {
                collider.sharedMesh = null;
                collider.sharedMesh = mesh;
                collider.convex = true;
            }
            else
            {
                // If had a BoxCollider or SphereCollider, replace with convex MeshCollider
                Collider existingCol = go.GetComponent<Collider>();
                if (existingCol != null && !(existingCol is MeshCollider))
                {
                    UnityEngine.Object.Destroy(existingCol);
                    MeshCollider newCol = go.AddComponent<MeshCollider>();
                    newCol.sharedMesh = mesh;
                    newCol.convex = true;
                }
            }
        }
    }
}
