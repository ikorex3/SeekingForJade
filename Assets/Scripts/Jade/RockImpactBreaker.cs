using SeekingForJade.Slicing;
using UnityEngine;

namespace SeekingForJade.Jade
{
    [RequireComponent(typeof(ProceduralRock), typeof(Rigidbody))]
    public class RockImpactBreaker : MonoBehaviour
    {
        [Header("Break Settings")]
        [SerializeField] private float minImpactVelocity = 3.2f;
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

        public void BreakOnImpact(Vector3 impactPoint, Vector3 surfaceNormal)
        {
            if (rock == null) rock = GetComponent<ProceduralRock>();
            if (rb == null) rb = GetComponent<Rigidbody>();

            if (hasBroken || (rock != null && rock.IsSliced)) return;
            hasBroken = true;

            if (jadeCapMaterial == null)
            {
#if UNITY_EDITOR
                jadeCapMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Jade_Internal.mat");
#else
                jadeCapMaterial = Resources.Load<Material>("M_Jade_Internal");
#endif
            }

            // Spawn dust & pebble impact burst VFX
            SeekingForJade.VFX.RockVFXManager.SpawnSmashImpactVFX(impactPoint, surfaceNormal);

            // Primary shock fracture along impact normal with jagged angle tilt
            Vector3 randomTilt = Random.insideUnitSphere * 0.45f;
            Vector3 cutNormal1 = (surfaceNormal + randomTilt).normalized;

            var slice1 = MeshSlicer.Slice(gameObject, impactPoint, cutNormal1, jadeCapMaterial);
            if (!slice1.success) return;

            GameObject pieceA = slice1.positiveSideObject;
            GameObject pieceB = slice1.negativeSideObject;

            ProceduralRock rockA = pieceA.GetComponent<ProceduralRock>();
            if (rockA != null) rockA.ApplyCrudeBreakPenalty();

            ProceduralRock rockB = pieceB.GetComponent<ProceduralRock>();
            if (rockB == null) rockB = pieceB.AddComponent<ProceduralRock>();
            rockB.CopyFromParent(rock, rock.WeightKg * 0.5f);
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
                rockC.ApplyCrudeBreakPenalty();
                DisarmBreaker(pieceC);

                pieceC.name = $"{gameObject.name}_Shard";
                Rigidbody rbC = pieceC.GetComponent<Rigidbody>();
                if (rbC != null) rbC.AddForce((cutNormal2 + Vector3.up * 0.8f) * 3.5f, ForceMode.Impulse);
            }

            // Apply scatter impulses
            Rigidbody rbA = pieceA.GetComponent<Rigidbody>();
            Rigidbody rbB = pieceB.GetComponent<Rigidbody>();
            if (rbA != null) rbA.AddForce((cutNormal1 + Vector3.up * 0.4f) * 2.8f, ForceMode.Impulse);
            if (rbB != null) rbB.AddForce((-cutNormal1 + Vector3.up * 0.4f) * 2.8f, ForceMode.Impulse);

            Debug.Log($"<color=orange>[RockImpactBreaker]</color> Rock smashed into multiple jagged pieces! Crude break penalty applied (-60% valuation).");
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
