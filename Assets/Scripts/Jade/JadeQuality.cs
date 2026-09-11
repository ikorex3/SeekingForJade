using System;
using UnityEngine;

namespace SeekingForJade.Jade
{
    public enum JadeRarity
    {
        BrickStone = 0,       // Worthless grey/white limestone rock
        BeanGreen = 1,        // Common, light opaque green
        AppleGreen = 2,       // Rare, vibrant solid emerald green
        Lavender = 3,         // Epic, rare purple/violet tone
        ImperialGlass = 4     // Legendary, hyper-translucent glowing deep emerald
    }

    [Serializable]
    public struct JadeQuality
    {
        public JadeRarity rarity;
        [Range(0f, 1f)] public float translucency;   // Light penetration capability
        [Range(0f, 1f)] public float purity;         // Freedom from dark spots/cotton
        [Range(0f, 1f)] public float crackSeverity;  // Structural fractures lowering value
        public Color primaryColor;
        public Color veinColor;
        public float basePricePerKg;

        public static JadeQuality Generate(JadeRarity rarity, int seed)
        {
            var rng = new System.Random(seed);
            float t = (float)rng.NextDouble();
            float p = (float)rng.NextDouble();
            float c = (float)rng.NextDouble();

            JadeQuality quality = new JadeQuality { rarity = rarity };

            switch (rarity)
            {
                case JadeRarity.BrickStone:
                    quality.translucency = Mathf.Lerp(0.02f, 0.1f, t);
                    quality.purity = Mathf.Lerp(0.1f, 0.4f, p);
                    quality.crackSeverity = Mathf.Lerp(0.2f, 0.8f, c);
                    quality.primaryColor = new Color(0.72f, 0.70f, 0.65f); // Ash grey/chalk
                    quality.veinColor = new Color(0.55f, 0.52f, 0.48f);
                    quality.basePricePerKg = 5f;
                    break;

                case JadeRarity.BeanGreen:
                    quality.translucency = Mathf.Lerp(0.15f, 0.35f, t);
                    quality.purity = Mathf.Lerp(0.4f, 0.75f, p);
                    quality.crackSeverity = Mathf.Lerp(0.1f, 0.5f, c);
                    quality.primaryColor = new Color(0.38f, 0.65f, 0.35f); // Muted pea green
                    quality.veinColor = new Color(0.25f, 0.48f, 0.22f);
                    quality.basePricePerKg = 80f;
                    break;

                case JadeRarity.AppleGreen:
                    quality.translucency = Mathf.Lerp(0.4f, 0.7f, t);
                    quality.purity = Mathf.Lerp(0.6f, 0.9f, p);
                    quality.crackSeverity = Mathf.Lerp(0.05f, 0.4f, c);
                    quality.primaryColor = new Color(0.15f, 0.78f, 0.32f); // Fresh vibrant green
                    quality.veinColor = new Color(0.08f, 0.55f, 0.20f);
                    quality.basePricePerKg = 450f;
                    break;

                case JadeRarity.Lavender:
                    quality.translucency = Mathf.Lerp(0.5f, 0.8f, t);
                    quality.purity = Mathf.Lerp(0.65f, 0.92f, p);
                    quality.crackSeverity = Mathf.Lerp(0.05f, 0.35f, c);
                    quality.primaryColor = new Color(0.65f, 0.45f, 0.78f); // Soft violet
                    quality.veinColor = new Color(0.45f, 0.25f, 0.60f);
                    quality.basePricePerKg = 900f;
                    break;

                case JadeRarity.ImperialGlass:
                    quality.translucency = Mathf.Lerp(0.85f, 1.0f, t);
                    quality.purity = Mathf.Lerp(0.85f, 0.99f, p);
                    quality.crackSeverity = Mathf.Lerp(0.0f, 0.2f, c);
                    quality.primaryColor = new Color(0.05f, 0.88f, 0.45f); // Glowing emerald
                    quality.veinColor = new Color(0.02f, 0.98f, 0.55f);
                    quality.basePricePerKg = 3500f;
                    break;
            }

            return quality;
        }

        public int CalculateValue(float weightKg, float exposedJadeRatio = 1f, float cutEfficiency = 1f)
        {
            float purityMultiplier = Mathf.Lerp(0.3f, 1.5f, purity);
            float translucencyMultiplier = Mathf.Lerp(0.5f, 2.0f, translucency);
            float crackPenalty = Mathf.Lerp(1.0f, 0.2f, crackSeverity);

            float totalValue = weightKg * basePricePerKg * purityMultiplier * translucencyMultiplier * crackPenalty * exposedJadeRatio * cutEfficiency;
            return Mathf.Max(5, Mathf.RoundToInt(totalValue));
        }
    }
}
