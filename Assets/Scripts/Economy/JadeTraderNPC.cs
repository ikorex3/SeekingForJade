using System;
using SeekingForJade.Jade;
using UnityEngine;

namespace SeekingForJade.Economy
{
    public class JadeTraderNPC : MonoBehaviour
    {
        [Header("Trader Identity")]
        [SerializeField] private string traderName = "Master Chen (Jade Merchant)";

        [Header("Scale & Sell Zone")]
        [SerializeField] private Transform scaleZone;
        [SerializeField] private float scaleCheckRadius = 1.2f;

        [Header("Shop Catalog")]
        [SerializeField] private RockData[] availableBoulders;
        [SerializeField] private int[] boulderPrices;
        [SerializeField] private Transform purchaseSpawnPoint;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip coinSound;

        public string TraderName => traderName;

        public bool TrySellPlacedSlices(out int totalEarnings, out int count)
        {
            totalEarnings = 0;
            count = 0;

            Vector3 center = scaleZone != null ? scaleZone.position : transform.position;
            Collider[] hits = Physics.OverlapSphere(center, scaleCheckRadius);

            foreach (var hit in hits)
            {
                ProceduralRock rock = hit.GetComponentInParent<ProceduralRock>();
                if (rock != null && rock.IsSliced)
                {
                    int value = rock.GetEstimatedValue();
                    totalEarnings += value;
                    count++;
                    Destroy(rock.gameObject);
                }
            }

            if (totalEarnings > 0)
            {
                PlayerWallet.Instance?.AddMoney(totalEarnings, $"Sold {count} Jade Piece{(count > 1 ? "s" : "")}");
                if (audioSource != null && coinSound != null)
                {
                    audioSource.PlayOneShot(coinSound);
                }
                return true;
            }

            return false;
        }

        public bool TryBuyBoulder(int catalogIndex, out GameObject boughtBoulder)
        {
            boughtBoulder = null;
            if (availableBoulders == null || catalogIndex < 0 || catalogIndex >= availableBoulders.Length)
                return false;

            int price = (boulderPrices != null && catalogIndex < boulderPrices.Length) 
                ? boulderPrices[catalogIndex] 
                : 100;

            if (PlayerWallet.Instance == null || !PlayerWallet.Instance.TrySpendMoney(price, availableBoulders[catalogIndex].rockName))
            {
                return false;
            }

            Vector3 spawnPos = purchaseSpawnPoint != null ? purchaseSpawnPoint.position : transform.position + Vector3.up * 0.8f;
            int seed = UnityEngine.Random.Range(100000, 999999);

            boughtBoulder = ProceduralRockGenerator.CreateRockGameObject(availableBoulders[catalogIndex], seed, spawnPos);

            if (audioSource != null && coinSound != null)
            {
                audioSource.PlayOneShot(coinSound);
            }

            return true;
        }

        public string GetAppraisalPrompt()
        {
            Vector3 center = scaleZone != null ? scaleZone.position : transform.position;
            Collider[] hits = Physics.OverlapSphere(center, scaleCheckRadius);

            int totalVal = 0;
            int count = 0;
            foreach (var hit in hits)
            {
                ProceduralRock rock = hit.GetComponentInParent<ProceduralRock>();
                if (rock != null && rock.IsSliced)
                {
                    totalVal += rock.GetEstimatedValue();
                    count++;
                }
            }

            if (count > 0)
            {
                return $"[E] Sell {count} Jade Piece{(count > 1 ? "s" : "")} on Scale for ${totalVal:N0}";
            }

            return "Scale Empty: Place Sliced Jade here to sell";
        }
    }
}
