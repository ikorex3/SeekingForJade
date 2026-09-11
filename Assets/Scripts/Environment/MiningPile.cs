using System;
using SeekingForJade.Jade;
using Unity.Netcode;
using UnityEngine;

namespace SeekingForJade.Environment
{
    public class MiningPile : NetworkBehaviour
    {
        [Header("Mining Settings")]
        [SerializeField] private RockData rawRockData;
        [SerializeField] private Transform rockSpawnPoint;

        [Header("Audio & FX")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip mineSound;

        public bool IsReady => true;
        public float RemainingCooldown => 0f;

        public event Action OnMined;

        public bool TryMineRock(out GameObject spawnedRock)
        {
            spawnedRock = null;

            if (audioSource != null && mineSound != null)
            {
                audioSource.PlayOneShot(mineSound);
            }

            int randomSeed = UnityEngine.Random.Range(100000, 999999);
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f;
            if (rockSpawnPoint != null && Vector3.Distance(rockSpawnPoint.position, transform.position) < 1.8f)
            {
                spawnPos = new Vector3(rockSpawnPoint.position.x, transform.position.y + 1.5f, rockSpawnPoint.position.z);
            }

            spawnedRock = ProceduralRockGenerator.CreateRockGameObject(rawRockData, randomSeed, spawnPos);

            // Ejection force
            Rigidbody rb = spawnedRock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 launchDir = (Vector3.up * 2.0f + UnityEngine.Random.insideUnitSphere * 0.4f).normalized;
                rb.AddForce(launchDir * 3.5f, ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 3.0f, ForceMode.Impulse);
            }

            OnMined?.Invoke();
            return true;
        }

        public string GetPromptText()
        {
            return "[E] Mine Raw Rock (Ready!)";
        }
    }
}
