using System;
using SeekingForJade.Jade;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace SeekingForJade.Jade
{
    public enum CutMethod
    {
        None = 0,
        PrecisionSaw = 1,
        RoughSmash = 2
    }

    [System.Serializable]
    public struct RockNetworkState : INetworkSerializable, IEquatable<RockNetworkState>
    {
        public int RockSeed;
        public FixedString64Bytes RockDataName;
        public float WeightKg;
        public JadeRarity Rarity;
        public float Translucency;
        public float Purity;
        public float CrackSeverity;
        public Color PrimaryColor;
        public Color VeinColor;
        public float BasePricePerKg;
        public bool IsInitialized;
        public bool IsSliced;
        public bool IsTapeWrapped;
        public CutMethod CutMethod;
        public float CutEfficiency;
        public bool IsMarketDisplay;
        public int MarketPrice;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RockSeed);
            serializer.SerializeValue(ref RockDataName);
            serializer.SerializeValue(ref WeightKg);
            serializer.SerializeValue(ref Rarity);
            serializer.SerializeValue(ref Translucency);
            serializer.SerializeValue(ref Purity);
            serializer.SerializeValue(ref CrackSeverity);
            serializer.SerializeValue(ref PrimaryColor);
            serializer.SerializeValue(ref VeinColor);
            serializer.SerializeValue(ref BasePricePerKg);
            serializer.SerializeValue(ref IsInitialized);
            serializer.SerializeValue(ref IsSliced);
            serializer.SerializeValue(ref IsTapeWrapped);
            serializer.SerializeValue(ref CutMethod);
            serializer.SerializeValue(ref CutEfficiency);
            serializer.SerializeValue(ref IsMarketDisplay);
            serializer.SerializeValue(ref MarketPrice);
        }

        public bool Equals(RockNetworkState other)
        {
            return RockSeed == other.RockSeed &&
                   RockDataName.Equals(other.RockDataName) &&
                   Mathf.Approximately(WeightKg, other.WeightKg) &&
                   Rarity == other.Rarity &&
                   Mathf.Approximately(Translucency, other.Translucency) &&
                   Mathf.Approximately(Purity, other.Purity) &&
                   Mathf.Approximately(CrackSeverity, other.CrackSeverity) &&
                   PrimaryColor == other.PrimaryColor &&
                   VeinColor == other.VeinColor &&
                   Mathf.Approximately(BasePricePerKg, other.BasePricePerKg) &&
                   IsInitialized == other.IsInitialized &&
                   IsSliced == other.IsSliced &&
                   IsTapeWrapped == other.IsTapeWrapped &&
                   CutMethod == other.CutMethod &&
                   Mathf.Approximately(CutEfficiency, other.CutEfficiency) &&
                   IsMarketDisplay == other.IsMarketDisplay &&
                   MarketPrice == other.MarketPrice;
        }
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
    public class ProceduralRock : NetworkBehaviour
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

        [Header("Network State")]
        public NetworkVariable<RockNetworkState> NetState = new(
            writePerm: NetworkVariableWritePermission.Server
        );

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

        public override void OnNetworkSpawn()
        {
            NetState.OnValueChanged += OnRockStateChanged;

            if (IsServer)
            {
                if (isInitialized)
                {
                    PushLocalStateToNetwork();
                }
                else if (rockData != null || rockSeed != 0)
                {
                    Initialize(rockData, rockSeed != 0 ? rockSeed : UnityEngine.Random.Range(100000, 999999));
                }
            }
            else
            {
                if (NetState.Value.IsInitialized)
                {
                    ApplyNetworkState(NetState.Value);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            NetState.OnValueChanged -= OnRockStateChanged;
        }

        private void OnRockStateChanged(RockNetworkState previousValue, RockNetworkState newValue)
        {
            if (!IsServer)
            {
                ApplyNetworkState(newValue);
            }
        }

        public void ApplyNetworkState(RockNetworkState state)
        {
            rockSeed = state.RockSeed;
            weightKg = state.WeightKg;
            quality.rarity = state.Rarity;
            quality.translucency = state.Translucency;
            quality.purity = state.Purity;
            quality.crackSeverity = state.CrackSeverity;
            quality.primaryColor = state.PrimaryColor;
            quality.veinColor = state.VeinColor;
            quality.basePricePerKg = state.BasePricePerKg;
            isInitialized = state.IsInitialized;
            isSliced = state.IsSliced;
            isTapeWrapped = state.IsTapeWrapped;
            cutMethod = state.CutMethod;
            cutEfficiency = state.CutEfficiency;
            isMarketDisplay = state.IsMarketDisplay;
            marketPrice = state.MarketPrice;

            if (rockData == null && !state.RockDataName.IsEmpty)
            {
                rockData = ProceduralRockGenerator.GetRockDataByName(state.RockDataName.ToString());
            }

            if (TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh == null && rockSeed != 0)
            {
                Mesh generatedMesh = ProceduralRockGenerator.GenerateRockMesh(rockSeed);
                filter.sharedMesh = generatedMesh;

                if (TryGetComponent<MeshCollider>(out var col))
                {
                    col.sharedMesh = generatedMesh;
                    col.convex = true;
                }
            }

            if (rockData != null && rockData.crustMaterial != null)
            {
                if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null && meshRenderer.sharedMaterial == null)
                {
                    meshRenderer.sharedMaterial = rockData.crustMaterial;
                }
            }

            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = isMarketDisplay;
            }

            ApplyVisualProperties();
        }

        public void PushLocalStateToNetwork()
        {
            if (!IsSpawned || !IsServer) return;

            FixedString64Bytes dataName = rockData != null ? new FixedString64Bytes(rockData.name) : default;
            NetState.Value = new RockNetworkState
            {
                RockSeed = rockSeed,
                RockDataName = dataName,
                WeightKg = weightKg,
                Rarity = quality.rarity,
                Translucency = quality.translucency,
                Purity = quality.purity,
                CrackSeverity = quality.crackSeverity,
                PrimaryColor = quality.primaryColor,
                VeinColor = quality.veinColor,
                BasePricePerKg = quality.basePricePerKg,
                IsInitialized = isInitialized,
                IsSliced = isSliced,
                IsTapeWrapped = isTapeWrapped,
                CutMethod = cutMethod,
                CutEfficiency = cutEfficiency,
                IsMarketDisplay = isMarketDisplay,
                MarketPrice = marketPrice
            };
        }

        public void SetTapeWrapped(bool wrapped)
        {
            isTapeWrapped = wrapped;
            ApplyVisualProperties();
            PushLocalStateToNetwork();
        }

        public void SetMarketDisplay(bool display, int price)
        {
            isMarketDisplay = display;
            marketPrice = price;

            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = display;
            }
            PushLocalStateToNetwork();
        }

        public void PurchaseFromMarket()
        {
            isMarketDisplay = false;
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
            }
            PushLocalStateToNetwork();
        }

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            // Local / Editor fallback when running outside of an active network session
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                if (!isInitialized)
                {
                    Initialize(rockData, UnityEngine.Random.Range(100000, 999999));
                }
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
            PushLocalStateToNetwork();
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
            PushLocalStateToNetwork();
        }

        public void MarkAsSawCut()
        {
            isSliced = true;
            cutMethod = CutMethod.PrecisionSaw;
            cutEfficiency = 1.0f;
            ApplyVisualProperties();
            PushLocalStateToNetwork();
        }

        public void MarkAsSliced()
        {
            isSliced = true;
            if (cutMethod == CutMethod.None)
            {
                cutMethod = CutMethod.PrecisionSaw;
                cutEfficiency = 1.0f;
            }
            PushLocalStateToNetwork();
        }

        public void ApplyCrudeBreakPenalty()
        {
            isSliced = true;
            cutMethod = CutMethod.RoughSmash;
            cutEfficiency = 0.40f; // 60% value penalty from crude shock fractures
            quality.crackSeverity = Mathf.Clamp01(quality.crackSeverity + 0.45f);
            ApplyVisualProperties();
            PushLocalStateToNetwork();
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
