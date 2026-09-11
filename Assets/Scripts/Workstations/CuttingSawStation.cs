using System;
using System.Collections;
using SeekingForJade.Jade;
using SeekingForJade.Slicing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SeekingForJade.Workstations
{
    public enum SawState
    {
        Idle,
        RockLoaded,
        Cutting,
        CutComplete
    }

    public class CuttingSawStation : MonoBehaviour
    {
        [Header("Mount Points & References")]
        [SerializeField] private Transform rockClampPoint;
        [SerializeField] private Transform sawArm;
        [SerializeField] private Transform sawBlade;
        [SerializeField] private Material jadeCapMaterial;

        [Header("Cutting Animation Settings")]
        [SerializeField] private float sawBladeSpinSpeed = 1200f;
        [SerializeField] private float cutDuration = 2.5f;
        [SerializeField] private float sawTravelDistance = 0.8f;
        [SerializeField] private Vector3 cutPlaneNormal = Vector3.right; // Slices along X plane

        [Header("Feedback & FX")]
        [SerializeField] private ParticleSystem waterDustFx;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip sawMotorSound;
        [SerializeField] private AudioClip sawCutSound;
        [SerializeField] private AudioClip sliceSuccessSound;

        [Header("Current State")]
        [SerializeField] private SawState state = SawState.Idle;
        [SerializeField] private ProceduralRock clampedRock;
        [SerializeField] private float cutProgress = 0f;

        private Vector3 initialSawArmPos;

        public SawState State => state;
        public ProceduralRock ClampedRock => clampedRock;

        public event Action<ProceduralRock> OnRockLoaded;
        public event Action<GameObject, GameObject> OnRockSliced;

        private void Awake()
        {
            if (sawArm != null)
            {
                initialSawArmPos = sawArm.localPosition;
            }
        }

        private void Update()
        {
            // Spin blade while cutting (local Y is cylinder axle)
            if (state == SawState.Cutting && sawBlade != null)
            {
                sawBlade.Rotate(Vector3.up, sawBladeSpinSpeed * Time.deltaTime, Space.Self);
            }
        }

        public bool TryLoadRock(ProceduralRock rock)
        {
            if (state != SawState.Idle || rock == null) return false;

            clampedRock = rock;
            state = SawState.RockLoaded;

            // Zero velocity BEFORE setting kinematic to avoid Unity warning
            Rigidbody rb = rock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            rock.transform.position = rockClampPoint != null ? rockClampPoint.position : transform.position;
            rock.transform.rotation = rockClampPoint != null ? rockClampPoint.rotation : transform.rotation;

            OnRockLoaded?.Invoke(rock);
            return true;
        }

        public void StartCut()
        {
            if (state != SawState.RockLoaded || clampedRock == null) return;

            StartCoroutine(ExecuteCutRoutine());
        }

        private IEnumerator ExecuteCutRoutine()
        {
            state = SawState.Cutting;
            cutProgress = 0f;

            if (waterDustFx != null) waterDustFx.Play();
            if (audioSource != null && sawCutSound != null)
            {
                audioSource.clip = sawCutSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            Vector3 startArmPos = initialSawArmPos;
            Vector3 targetArmPos = initialSawArmPos + Vector3.down * sawTravelDistance;

            while (cutProgress < 1f)
            {
                cutProgress += Time.deltaTime / cutDuration;
                if (sawArm != null)
                {
                    sawArm.localPosition = Vector3.Lerp(startArmPos, targetArmPos, cutProgress);
                }
                yield return null;
            }

            // Perform dynamic mesh slice at clamp center
            PerformSlice();

            // Retract saw arm
            float retractProgress = 0f;
            while (retractProgress < 1f)
            {
                retractProgress += Time.deltaTime / 0.8f;
                if (sawArm != null)
                {
                    sawArm.localPosition = Vector3.Lerp(targetArmPos, startArmPos, retractProgress);
                }
                yield return null;
            }

            if (waterDustFx != null) waterDustFx.Stop();
            if (audioSource != null)
            {
                audioSource.Stop();
                if (sliceSuccessSound != null)
                {
                    audioSource.PlayOneShot(sliceSuccessSound);
                }
            }

            state = SawState.CutComplete;
        }

        private void PerformSlice()
        {
            if (clampedRock == null) return;

            GameObject rockObj = clampedRock.gameObject;
            Vector3 slicePoint = rockClampPoint != null ? rockClampPoint.position : rockObj.transform.position;
            Vector3 worldNormal = transform.TransformDirection(cutPlaneNormal).normalized;

            // Execute dynamic mesh slice
            var sliceResult = MeshSlicer.Slice(rockObj, slicePoint, worldNormal, jadeCapMaterial);

            if (sliceResult.success)
            {
                GameObject pieceA = sliceResult.positiveSideObject;
                GameObject pieceB = sliceResult.negativeSideObject;

                // Configure piece A
                ProceduralRock rockA = pieceA.GetComponent<ProceduralRock>();
                if (rockA != null)
                {
                    rockA.MarkAsSawCut();
                }

                // Configure piece B
                ProceduralRock rockB = pieceB.GetComponent<ProceduralRock>();
                if (rockB == null)
                {
                    rockB = pieceB.AddComponent<ProceduralRock>();
                }
                rockB.CopyFromParent(clampedRock, clampedRock.WeightKg * 0.5f);
                rockB.MarkAsSawCut();

                // Unfreeze rigidbodies so the cut pieces drop onto table
                Rigidbody rbA = pieceA.GetComponent<Rigidbody>();
                Rigidbody rbB = pieceB.GetComponent<Rigidbody>();
                if (rbA != null) rbA.isKinematic = false;
                if (rbB != null) rbB.isKinematic = false;

                OnRockSliced?.Invoke(pieceA, pieceB);
            }

            clampedRock = null;
        }

        public void ResetStation()
        {
            state = SawState.Idle;
            clampedRock = null;
            cutProgress = 0f;
            if (sawArm != null) sawArm.localPosition = initialSawArmPos;
        }
    }
}
