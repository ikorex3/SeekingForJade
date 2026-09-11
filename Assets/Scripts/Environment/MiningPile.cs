using System;
using SeekingForJade.Jade;
using UnityEngine;

namespace SeekingForJade.Environment
{
    public class MiningPile : MonoBehaviour
    {
        [Header("Mining Settings")]
        [SerializeField] private float cooldownSeconds = 15f;
        [SerializeField] private RockData rawRockData;
        [SerializeField] private Transform rockSpawnPoint;

        [Header("State")]
        [SerializeField] private float remainingCooldown = 0f;

        [Header("Audio & FX")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip mineSound;

        public bool IsReady => remainingCooldown <= 0f;
        public float RemainingCooldown => remainingCooldown;

        public event Action OnMined;

        private void Update()
        {
            if (remainingCooldown > 0f)
            {
                remainingCooldown -= Time.deltaTime;
                if (remainingCooldown < 0f)
                {
                    remainingCooldown = 0f;
                }
            }
        }

        public bool TryMineRock(out GameObject spawnedRock)
        {
            spawnedRock = null;
            if (!IsReady) return false;

            remainingCooldown = cooldownSeconds;

            if (audioSource != null && mineSound != null)
            {
                audioSource.PlayOneShot(mineSound);
            }

            int randomSeed = UnityEngine.Random.Range(100000, 999999);
            Vector3 spawnPos = rockSpawnPoint != null ? rockSpawnPoint.position : transform.position + Vector3.up * 0.5f;

            spawnedRock = ProceduralRockGenerator.CreateRockGameObject(rawRockData, randomSeed, spawnPos);

            // Add small ejection velocity
            Rigidbody rb = spawnedRock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 launchDir = (Vector3.up * 1.5f + UnityEngine.Random.insideUnitSphere * 0.5f).normalized;
                rb.AddForce(launchDir * 2.5f, ForceMode.Impulse);
            }

            OnMined?.Invoke();
            return true;
        }

        public string GetPromptText()
        {
            if (IsReady)
            {
                return "[E] Mine Raw Rock (Ready!)";
            }
            return $"Mining Quarry: Cooling Down ({remainingCooldown:F0}s)";
        }
    }
}
