using SeekingForJade.Slicing;
using UnityEngine;

namespace SeekingForJade.Jade
{
    [RequireComponent(typeof(ProceduralRock), typeof(Rigidbody))]
    public class RockImpactBreaker : MonoBehaviour
    {
        [Header("Break Settings")]
        [SerializeField] private float minImpactVelocity = 1.5f;
        [SerializeField] private Material jadeCapMaterial;
        [SerializeField] private bool isArmed = false;

        private ProceduralRock rock;
        private Rigidbody rb;
        private bool hasBroken = false;

        public bool IsArmed
        {
            get => isArmed;
            set => isArmed = value;
        }

        public Material JadeCapMaterial
        {
            get => jadeCapMaterial;
            set => jadeCapMaterial = value;
        }

        private void Awake()
        {
            rock = GetComponent<ProceduralRock>();
            rb = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasBroken || !isArmed || rock.IsSliced) return;

            float impactSpeed = collision.relativeVelocity.magnitude;

            if (impactSpeed >= minImpactVelocity && collision.contactCount > 0)
            {
                ContactPoint contact = collision.contacts[0];
                BreakOnImpact(contact.point, contact.normal);
            }
        }

        public void ResetBreaker()
        {
            hasBroken = false;
            isArmed = false;
        }

        public void BreakOnImpact(Vector3 impactPoint, Vector3 surfaceNormal)
        {
            if (rock == null) rock = GetComponent<ProceduralRock>();
            if (rb == null) rb = GetComponent<Rigidbody>();

            if (hasBroken || (rock != null && rock.IsSliced)) return;

            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && !mf.sharedMesh.isReadable)
            {
                Mesh generatedMesh = ProceduralRockGenerator.GenerateMeshForSeed(rock != null ? rock.Seed : Random.Range(100000, 999999));
                mf.sharedMesh = generatedMesh;
                if (TryGetComponent<MeshCollider>(out var col))
                {
                    col.sharedMesh = generatedMesh;
                    col.convex = true;
                }
            }

            hasBroken = true;

            if (jadeCapMaterial == null)
            {
#if UNITY_EDITOR
                jadeCapMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Jade_Internal.mat");
#else
                jadeCapMaterial = Resources.Load<Material>("M_Jade_Internal");
#endif
            }

            // Spawn dust & pebble impact burst VFX safely
            try
            {
                SeekingForJade.VFX.RockVFXManager.SpawnSmashImpactVFX(impactPoint, surfaceNormal);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RockImpactBreaker] VFX Spawn warning: {ex.Message}");
            }

            // Primary shock fracture along impact normal with jagged angle tilt
            Vector3 randomTilt = Random.insideUnitSphere * 0.45f;
            Vector3 cutNormal1 = (surfaceNormal + randomTilt).normalized;

            var slice1 = MeshSlicer.Slice(gameObject, impactPoint, cutNormal1, jadeCapMaterial);
            if (!slice1.success) return;

            GameObject pieceA = slice1.positiveSideObject;
            GameObject pieceB = slice1.negativeSideObject;

            ProceduralRock rockA = pieceA.GetComponent<ProceduralRock>();
            if (rockA != null)
            {
                rockA.SetSliceData(slice1.localPlanePoint, slice1.localPlaneNormal, true);
                rockA.ApplyCrudeBreakPenalty();
            }

            ProceduralRock rockB = pieceB.GetComponent<ProceduralRock>();
            if (rockB == null) rockB = pieceB.AddComponent<ProceduralRock>();
            rockB.CopyFromParent(rock, rock.WeightKg * 0.5f);
            rockB.SetSliceData(slice1.localPlanePoint, slice1.localPlaneNormal, false);
            rockB.ApplyCrudeBreakPenalty();

            // Unarm main pieces
            DisarmBreaker(pieceA);
            DisarmBreaker(pieceB);

            // Secondary fracture: Chip off a broken corner shard from pieceA to create multi-part shattered break
            GameObject pieceC = null;
            Vector3 shardCutPoint = pieceA.transform.position + Random.insideUnitSphere * 0.15f;
            Vector3 cutNormal2 = (Vector3.Cross(cutNormal1, Vector3.up) + Random.insideUnitSphere * 0.3f).normalized;

            var slice2 = MeshSlicer.Slice(pieceA, shardCutPoint, cutNormal2, jadeCapMaterial);
            if (slice2.success)
            {
                // pieceA remains as positive, pieceC is the newly separated shard
                pieceC = slice2.negativeSideObject;
                ProceduralRock rockC = pieceC.GetComponent<ProceduralRock>();
                if (rockC == null) rockC = pieceC.AddComponent<ProceduralRock>();
                rockC.CopyFromParent(rockA, rockA.WeightKg * 0.3f);
                rockC.SetSliceData(slice2.localPlanePoint, slice2.localPlaneNormal, false);
                rockC.ApplyCrudeBreakPenalty();
                DisarmBreaker(pieceC);

                if (rockA != null)
                {
                    rockA.SetSliceData(slice2.localPlanePoint, slice2.localPlaneNormal, true);
                }

                pieceC.name = $"{gameObject.name}_Shard";
                Rigidbody rbC = pieceC.GetComponent<Rigidbody>();
                if (rbC != null) rbC.AddForce((cutNormal2 + Vector3.up * 0.8f) * 3.5f, ForceMode.Impulse);
            }

            // Apply scatter impulses
            Rigidbody rbA = pieceA.GetComponent<Rigidbody>();
            Rigidbody rbB = pieceB.GetComponent<Rigidbody>();
            if (rbA != null) rbA.AddForce((cutNormal1 + Vector3.up * 0.4f) * 2.8f, ForceMode.Impulse);
            if (rbB != null) rbB.AddForce((-cutNormal1 + Vector3.up * 0.4f) * 2.8f, ForceMode.Impulse);

            // Network spawn shattered pieces in multiplayer
            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening && Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                EnsureNetworkObjectAndSpawn(pieceA);
                EnsureNetworkObjectAndSpawn(pieceB);
                if (pieceC != null) EnsureNetworkObjectAndSpawn(pieceC);
            }

            Debug.Log($"<color=orange>[RockImpactBreaker]</color> Rock smashed into multiple jagged pieces! Crude break penalty applied (-60% valuation).");
        }

        private void EnsureNetworkObjectAndSpawn(GameObject obj)
        {
            if (obj == null) return;

            if (!obj.TryGetComponent<Unity.Netcode.NetworkObject>(out var netObj))
            {
                netObj = obj.AddComponent<Unity.Netcode.NetworkObject>();
            }
            if (!obj.TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var netTransform))
            {
                netTransform = obj.AddComponent<Unity.Netcode.Components.NetworkTransform>();
                netTransform.InLocalSpace = false;
                netTransform.Interpolate = true;
            }

            if (!netObj.IsSpawned)
            {
                netObj.Spawn();
                if (obj.TryGetComponent<ProceduralRock>(out var procRock))
                {
                    procRock.PushLocalStateToNetwork();
                }
            }
        }

        private void DisarmBreaker(GameObject obj)
        {
            RockImpactBreaker breaker = obj.GetComponent<RockImpactBreaker>();
            if (breaker != null)
            {
                breaker.isArmed = false;
                breaker.hasBroken = true;
            }
        }
    }
}
