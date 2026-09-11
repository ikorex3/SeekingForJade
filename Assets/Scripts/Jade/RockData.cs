using System;
using UnityEngine;

namespace SeekingForJade.Jade
{
    [Serializable]
    public struct RarityWeight
    {
        public JadeRarity rarity;
        [Range(0, 100)] public int weight;
    }

    [CreateAssetMenu(fileName = "NewRockData", menuName = "SeekingForJade/Rock Data")]
    public class RockData : ScriptableObject
    {
        [Header("Identity & Origin")]
        public string rockName = "Hpakant River Boulder";
        [TextArea] public string description = "A raw boulder harvested from northern riverbeds. Thick weathered yellowish-brown skin with potential jade core.";
        
        [Header("Physical Attributes")]
        public float minWeightKg = 1.5f;
        public float maxWeightKg = 12.0f;
        public float basePurchasePrice = 250f;

        [Header("Visuals & Materials")]
        public Material crustMaterial;
        public Color crustTint = new Color(0.55f, 0.45f, 0.35f);
        public bool isTapeWrappedDefault = false;

        [Header("Rarity Distribution")]
        public RarityWeight[] rarityTable = new RarityWeight[]
        {
            new RarityWeight { rarity = JadeRarity.BrickStone, weight = 50 },
            new RarityWeight { rarity = JadeRarity.BeanGreen, weight = 30 },
            new RarityWeight { rarity = JadeRarity.AppleGreen, weight = 14 },
            new RarityWeight { rarity = JadeRarity.Lavender, weight = 5 },
            new RarityWeight { rarity = JadeRarity.ImperialGlass, weight = 1 }
        };

        public JadeRarity RollRarity(int seed)
        {
            if (rarityTable == null || rarityTable.Length == 0)
                return JadeRarity.BrickStone;

            int totalWeight = 0;
            foreach (var entry in rarityTable)
            {
                totalWeight += entry.weight;
            }

            if (totalWeight <= 0) return JadeRarity.BrickStone;

            var rng = new System.Random(seed);
            int roll = rng.Next(0, totalWeight);
            int cumulative = 0;

            foreach (var entry in rarityTable)
            {
                cumulative += entry.weight;
                if (roll < cumulative)
                {
                    return entry.rarity;
                }
            }

            return rarityTable[0].rarity;
        }
    }
}
