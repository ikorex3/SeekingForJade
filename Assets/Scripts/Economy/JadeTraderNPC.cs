using System;
using System.Collections.Generic;
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

        [Header("Shop Catalog & Display Table")]
        [SerializeField] private RockData[] availableBoulders;
        [SerializeField] private int[] boulderPrices;
        [SerializeField] private Transform[] displaySlots;
        [SerializeField] private Transform purchaseSpawnPoint;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip coinSound;

        private ProceduralRock[] activeDisplayRocks;
        private float[] restockTimers;
        private const float RESTOCK_DELAY = 12f;

        public string TraderName => traderName;

        private void Start()
        {
            InitializeDisplayTable();
        }

        private void Update()
        {
            // Handle automatic restock timers for purchased slots
            if (displaySlots == null || displaySlots.Length == 0) return;

            for (int i = 0; i < displaySlots.Length; i++)
            {
                if (activeDisplayRocks[i] == null)
                {
                    restockTimers[i] -= Time.deltaTime;
                    if (restockTimers[i] <= 0f)
                    {
                        RestockSlot(i);
                    }
                }
            }
        }

        public void InitializeDisplayTable()
        {
            if (displaySlots == null || displaySlots.Length == 0) return;

            activeDisplayRocks = new ProceduralRock[displaySlots.Length];
            restockTimers = new float[displaySlots.Length];

            for (int i = 0; i < displaySlots.Length; i++)
            {
                RestockSlot(i);
            }
        }

        private void RestockSlot(int slotIndex)
        {
            if (displaySlots == null || slotIndex < 0 || slotIndex >= displaySlots.Length) return;
            if (availableBoulders == null || availableBoulders.Length == 0) return;

            Transform slot = displaySlots[slotIndex];
            if (slot == null) return;

            // Pick a boulder type based on slot index or catalog
            int catIdx = slotIndex % availableBoulders.Length;
            RockData data = availableBoulders[catIdx];
            int price = (boulderPrices != null && catIdx < boulderPrices.Length) ? boulderPrices[catIdx] : 200;

            int seed = UnityEngine.Random.Range(100000, 999999);
            GameObject rockObj = ProceduralRockGenerator.CreateRockGameObject(data, seed, slot.position);
            rockObj.transform.SetParent(transform);
            rockObj.transform.rotation = slot.rotation;

            ProceduralRock rock = rockObj.GetComponent<ProceduralRock>();
            if (rock != null)
            {
                rock.SetMarketDisplay(true, price);
                activeDisplayRocks[slotIndex] = rock;
            }
        }

        public bool TryBuyDisplayRock(ProceduralRock rock)
        {
            if (rock == null || !rock.IsMarketDisplay) return false;

            int price = rock.MarketPrice;
            string stoneName = rock.Data != null ? rock.Data.rockName : "Rough Boulder";

            if (PlayerWallet.Instance == null || !PlayerWallet.Instance.TrySpendMoney(price, stoneName))
            {
                Debug.LogWarning("[JadeTraderNPC] Player cannot afford boulder.");
                return false;
            }

            // Find and clear slot
            if (activeDisplayRocks != null)
            {
                for (int i = 0; i < activeDisplayRocks.Length; i++)
                {
                    if (activeDisplayRocks[i] == rock)
                    {
                        activeDisplayRocks[i] = null;
                        restockTimers[i] = RESTOCK_DELAY;
                        break;
                    }
                }
            }

            rock.PurchaseFromMarket();

            if (audioSource != null && coinSound != null)
            {
                audioSource.PlayOneShot(coinSound);
            }

            Debug.Log($"<color=yellow>[JadeTraderNPC]</color> Master Chen: 'Good eye, traveler! May that stone bring you a green fortune!'");
            return true;
        }

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

        public string GetAppraisalPrompt()
        {
            Vector3 center = scaleZone != null ? scaleZone.position : transform.position;
            Collider[] hits = Physics.OverlapSphere(center, scaleCheckRadius);

            int totalVal = 0;
            int sawCount = 0;
            int smashCount = 0;

            foreach (var hit in hits)
            {
                ProceduralRock rock = hit.GetComponentInParent<ProceduralRock>();
                if (rock != null && rock.IsSliced)
                {
                    totalVal += rock.GetEstimatedValue();
                    if (rock.Method == CutMethod.RoughSmash) smashCount++;
                    else sawCount++;
                }
            }

            int count = sawCount + smashCount;
            if (count > 0)
            {
                string breakdown = "";
                if (sawCount > 0 && smashCount > 0)
                {
                    breakdown = $" ({sawCount} Clean Saw / {smashCount} Smashed)";
                }
                else if (smashCount > 0)
                {
                    breakdown = " (Rough Smashed: -60% Value Penalty!)";
                }
                else
                {
                    breakdown = " (Precision Saw: Full Value)";
                }

                return $"[E] Sell {count} Jade Piece{(count > 1 ? "s" : "")}{breakdown} for ${totalVal:N0}";
            }

            return "Scale Empty: Place Sliced Jade here to sell";
        }
    }
}
