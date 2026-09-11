using UnityEngine;

namespace SeekingForJade.Jade
{
    public enum CutMethod
    {
        None = 0,
        PrecisionSaw = 1,
        RoughSmash = 2
    }

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

        [Header("Cutting & Valuation")]
        [SerializeField] private CutMethod cutMethod = CutMethod.None;
        [SerializeField] private float cutEfficiency = 1.0f;

        [Header("Market Display")]
        [SerializeField] private bool isMarketDisplay = false;
        [SerializeField] private int marketPrice = 0;

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;

        // Subsurface optical inspection light
        private GameObject haloObj;
        private Light haloLight;
        private float haloTimer = 0f;

        public RockData Data => rockData;
        public int Seed => rockSeed;
        public float WeightKg => weightKg;
        public JadeQuality Quality => quality;
        public bool IsSliced => isSliced;
        public bool IsTapeWrapped => isTapeWrapped;
        public CutMethod Method => cutMethod;
        public float CutEfficiency => cutEfficiency;
        public bool IsMarketDisplay => isMarketDisplay;
        public int MarketPrice => marketPrice;

        public void SetTapeWrapped(bool wrapped)
        {
            isTapeWrapped = wrapped;
            ApplyVisualProperties();
        }

        public void SetMarketDisplay(bool display, int price)
        {
            isMarketDisplay = display;
            marketPrice = price;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = display;
            }
        }

        public void PurchaseFromMarket()
        {
            isMarketDisplay = false;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
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

        private void Update()
        {
            if (haloTimer > 0f)
            {
                haloTimer -= Time.deltaTime;
                if (haloTimer <= 0f && haloLight != null)
                {
                    haloLight.enabled = false;
                }
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
            cutMethod = parent.cutMethod;
            cutEfficiency = parent.cutEfficiency;
            isInitialized = true;
            ApplyVisualProperties();
        }

        public void MarkAsSawCut()
        {
            isSliced = true;
            cutMethod = CutMethod.PrecisionSaw;
            cutEfficiency = 1.0f;
            ApplyVisualProperties();
        }

        public void MarkAsSliced()
        {
            isSliced = true;
            if (cutMethod == CutMethod.None)
            {
                cutMethod = CutMethod.PrecisionSaw;
                cutEfficiency = 1.0f;
            }
        }

        public void ApplyCrudeBreakPenalty()
        {
            isSliced = true;
            cutMethod = CutMethod.RoughSmash;
            cutEfficiency = 0.40f; // 60% value penalty from crude shock fractures
            quality.crackSeverity = Mathf.Clamp01(quality.crackSeverity + 0.45f);
            ApplyVisualProperties();
        }

        /// <summary>
        /// Real-time optical inspection. Projects a subsurface halo through the crust
        /// reflecting internal jade color saturation and translucency depth.
        /// </summary>
        public void InspectWithLight(Vector3 hitPoint, Vector3 hitNormal, Color torchColor, float torchIntensity, int mode)
        {
            if (isTapeWrapped)
            {
                if (haloLight != null) haloLight.enabled = false;
                return;
            }

            if (haloObj == null)
            {
                haloObj = new GameObject("SubsurfaceOpticalHalo");
                haloObj.transform.SetParent(transform);
                haloLight = haloObj.AddComponent<Light>();
                haloLight.type = LightType.Point;
                haloLight.shadows = LightShadows.None;
            }

            // Position optical halo just slightly inside the crust at contact point
            haloObj.transform.position = hitPoint - hitNormal * 0.03f;

            // Halo color is tinted by the internal jade hue, blended with the flashlight beam
            Color haloColor = Color.Lerp(quality.primaryColor, torchColor, 0.25f);
            haloLight.color = haloColor;
            haloLight.range = Mathf.Lerp(0.25f, 1.15f, quality.translucency);
            haloLight.intensity = Mathf.Lerp(1.2f, 4.5f, quality.translucency);
            haloLight.enabled = true;

            haloTimer = 0.15f;
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
            return quality.CalculateValue(weightKg, isSliced ? 1.0f : 0.2f, cutEfficiency);
        }
    }
}
