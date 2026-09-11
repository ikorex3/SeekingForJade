using UnityEngine;

namespace SeekingForJade.Jade
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ProceduralRock : MonoBehaviour
    {
        [Header("Data Definition")]
        [SerializeField] private RockData rockData;
        [SerializeField] private int rockSeed;

        [Header("Generated Attributes")]
        [SerializeField] private float weightKg;
        [SerializeField] private JadeQuality quality;
        [SerializeField] private bool isInitialized = false;
        [SerializeField] private bool isSliced = false;
        [SerializeField] private bool isTapeWrapped = false;

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;

        public RockData Data => rockData;
        public int Seed => rockSeed;
        public float WeightKg => weightKg;
        public JadeQuality Quality => quality;
        public bool IsSliced => isSliced;
        public bool IsTapeWrapped => isTapeWrapped;

        public void SetTapeWrapped(bool wrapped)
        {
            isTapeWrapped = wrapped;
            ApplyVisualProperties();
        }

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (!isInitialized)
            {
                Initialize(rockData, Random.Range(100000, 999999));
            }
        }

        public void Initialize(RockData data, int seed)
        {
            rockData = data;
            rockSeed = seed;

            var rng = new System.Random(seed);
            if (rockData != null)
            {
                weightKg = Mathf.Lerp(rockData.minWeightKg, rockData.maxWeightKg, (float)rng.NextDouble());
                JadeRarity rolledRarity = rockData.RollRarity(seed + 17);
                quality = JadeQuality.Generate(rolledRarity, seed + 99);
                isTapeWrapped = rockData.isTapeWrappedDefault;
            }
            else
            {
                weightKg = 5.0f;
                quality = JadeQuality.Generate(JadeRarity.BeanGreen, seed);
            }

            ApplyVisualProperties();
            isInitialized = true;
        }

        public void CopyFromParent(ProceduralRock parent, float sliceWeight)
        {
            rockData = parent.rockData;
            rockSeed = parent.rockSeed;
            quality = parent.quality;
            weightKg = sliceWeight;
            isTapeWrapped = parent.isTapeWrapped;
            isSliced = true;
            isInitialized = true;
            ApplyVisualProperties();
        }

        public void MarkAsSliced()
        {
            isSliced = true;
        }

        public void ApplyCrudeBreakPenalty()
        {
            isSliced = true;
            quality.crackSeverity = Mathf.Clamp01(quality.crackSeverity + 0.35f);
            quality.basePricePerKg *= 0.5f;
            ApplyVisualProperties();
        }

        public void ApplyVisualProperties()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            meshRenderer.GetPropertyBlock(propertyBlock);

            // Jade shader properties for internal cut face / subsurface reflection
            propertyBlock.SetColor("_JadePrimaryColor", quality.primaryColor);
            propertyBlock.SetColor("_JadeVeinColor", quality.veinColor);
            propertyBlock.SetFloat("_JadeTranslucency", quality.translucency);
            propertyBlock.SetFloat("_JadePurity", quality.purity);
            propertyBlock.SetFloat("_JadeCracks", quality.crackSeverity);

            meshRenderer.SetPropertyBlock(propertyBlock);

            // Submesh 0 (outer skin): If tape wrapped, apply glossy yellow packing tape
            if (isTapeWrapped)
            {
                MaterialPropertyBlock crustBlock = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(crustBlock, 0);
                Color tapeColor = new Color(0.92f, 0.72f, 0.18f, 1.0f); // Industrial packing tape yellow
                crustBlock.SetColor("_BaseColor", tapeColor);
                crustBlock.SetColor("_Color", tapeColor);
                crustBlock.SetFloat("_Smoothness", 0.75f);
                meshRenderer.SetPropertyBlock(crustBlock, 0);
            }

            // Submesh 1 (cut face): If sliced, explicitly set cap colors for solid jade rendering
            if (isSliced && meshRenderer.sharedMaterials != null && meshRenderer.sharedMaterials.Length > 1)
            {
                MaterialPropertyBlock capBlock = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(capBlock, 1);
                capBlock.SetColor("_BaseColor", quality.primaryColor);
                capBlock.SetColor("_Color", quality.primaryColor);
                capBlock.SetColor("_JadePrimaryColor", quality.primaryColor);
                capBlock.SetColor("_JadeVeinColor", quality.veinColor);
                capBlock.SetFloat("_JadeTranslucency", quality.translucency);
                capBlock.SetFloat("_JadePurity", quality.purity);
                capBlock.SetFloat("_JadeCracks", quality.crackSeverity);
                meshRenderer.SetPropertyBlock(capBlock, 1);
            }
        }

        public int GetEstimatedValue()
        {
            return quality.CalculateValue(weightKg, isSliced ? 1.0f : 0.2f);
        }
    }
}
