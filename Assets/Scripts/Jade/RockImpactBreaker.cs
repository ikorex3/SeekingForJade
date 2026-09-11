using SeekingForJade.Slicing;
using UnityEngine;

namespace SeekingForJade.Jade
{
    [RequireComponent(typeof(ProceduralRock), typeof(Rigidbody))]
    public class RockImpactBreaker : MonoBehaviour
    {
        [Header("Break Settings")]
        [SerializeField] private float minImpactVelocity = 3.8f;
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
            if (hasBroken || rock.IsSliced) return;
            hasBroken = true;

            // Generate an irregular break plane roughly perpendicular to surface normal with random tilt
            Vector3 randomTilt = Random.insideUnitSphere * 0.35f;
            Vector3 cutNormal = (surfaceNormal + randomTilt).normalized;

            if (jadeCapMaterial == null)
            {
                jadeCapMaterial = Resources.Load<Material>("M_Jade_Internal");
            }

            var sliceResult = MeshSlicer.Slice(gameObject, impactPoint, cutNormal, jadeCapMaterial);

            if (sliceResult.success)
            {
                GameObject pieceA = sliceResult.positiveSideObject;
                GameObject pieceB = sliceResult.negativeSideObject;

                ProceduralRock rockA = pieceA.GetComponent<ProceduralRock>();
                if (rockA != null)
                {
                    rockA.ApplyCrudeBreakPenalty();
                }

                ProceduralRock rockB = pieceB.GetComponent<ProceduralRock>();
                if (rockB == null)
                {
                    rockB = pieceB.AddComponent<ProceduralRock>();
                }
                rockB.CopyFromParent(rock, rock.WeightKg * 0.5f);
                rockB.ApplyCrudeBreakPenalty();

                // Unarm both pieces so they don't break again immediately
                RockImpactBreaker breakerA = pieceA.GetComponent<RockImpactBreaker>();
                if (breakerA != null) breakerA.isArmed = false;

                RockImpactBreaker breakerB = pieceB.GetComponent<RockImpactBreaker>();
                if (breakerB != null) breakerB.isArmed = false;

                // Apply dynamic scatter impulse
                Rigidbody rbA = pieceA.GetComponent<Rigidbody>();
                Rigidbody rbB = pieceB.GetComponent<Rigidbody>();
                if (rbA != null) rbA.AddForce((cutNormal + Vector3.up * 0.5f) * 2.5f, ForceMode.Impulse);
                if (rbB != null) rbB.AddForce((-cutNormal + Vector3.up * 0.5f) * 2.5f, ForceMode.Impulse);
            }
        }
    }
}
