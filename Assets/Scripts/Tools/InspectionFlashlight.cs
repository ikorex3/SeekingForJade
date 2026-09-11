using SeekingForJade.Jade;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SeekingForJade.Tools
{
    public class InspectionFlashlight : MonoBehaviour
    {
        [Header("Light Components")]
        [SerializeField] private Light spotLight;
        [SerializeField] private float maxInspectionDistance = 2.8f;

        [Header("Modes")]
        [SerializeField] private Color warmYellowLight = new Color(1.0f, 0.88f, 0.55f);
        [SerializeField] private Color coolWhiteLight = new Color(0.85f, 0.95f, 1.0f);
        [SerializeField] private Color uvPurpleLight = new Color(0.6f, 0.2f, 1.0f);
        [SerializeField] private int currentMode = 0; // 0 = warm, 1 = white, 2 = uv

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;

        // Active inspection state
        private ProceduralRock inspectedRock;
        private bool isInspectingTapeRock = false;

        public bool IsOn => spotLight != null && spotLight.enabled;
        public int CurrentMode => currentMode;

        private void Awake()
        {
            if (spotLight == null)
            {
                spotLight = GetComponentInChildren<Light>();
            }
            ApplyLightMode();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Toggle flashlight On/Off with 'F'
            if (keyboard.fKey.wasPressedThisFrame)
            {
                TogglePower();
            }

            // Cycle modes with 'T' (Warm Yellow -> Cool White -> UV)
            if (keyboard.tKey.wasPressedThisFrame && spotLight != null && spotLight.enabled)
            {
                CycleMode();
            }

            // Raycast ahead to detect rock surface contact
            if (spotLight != null && spotLight.enabled)
            {
                InspectRaycast();
            }
            else
            {
                inspectedRock = null;
                isInspectingTapeRock = false;
            }
        }

        public void TogglePower()
        {
            if (spotLight != null)
            {
                spotLight.enabled = !spotLight.enabled;
                if (audioSource != null && clickSound != null)
                {
                    audioSource.PlayOneShot(clickSound);
                }
            }
        }

        public void CycleMode()
        {
            currentMode = (currentMode + 1) % 3;
            ApplyLightMode();
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }

        private void ApplyLightMode()
        {
            if (spotLight == null) return;

            switch (currentMode)
            {
                case 0:
                    spotLight.color = warmYellowLight;
                    spotLight.spotAngle = 24f;
                    spotLight.innerSpotAngle = 10f;
                    spotLight.intensity = 1800f;
                    break;
                case 1:
                    spotLight.color = coolWhiteLight;
                    spotLight.spotAngle = 30f;
                    spotLight.innerSpotAngle = 14f;
                    spotLight.intensity = 2200f;
                    break;
                case 2:
                    spotLight.color = uvPurpleLight;
                    spotLight.spotAngle = 26f;
                    spotLight.innerSpotAngle = 12f;
                    spotLight.intensity = 1600f;
                    break;
            }
        }

        private void InspectRaycast()
        {
            inspectedRock = null;
            isInspectingTapeRock = false;

            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxInspectionDistance))
            {
                ProceduralRock rock = hit.collider.GetComponentInParent<ProceduralRock>();
                if (rock != null)
                {
                    inspectedRock = rock;
                    if (rock.IsTapeWrapped)
                    {
                        isInspectingTapeRock = true;
                        spotLight.innerSpotAngle = 5f;
                        return;
                    }

                    // Contact proximity & alignment
                    float proximity = 1.0f - Mathf.Clamp01(hit.distance / maxInspectionDistance);
                    float alignment = Mathf.Clamp01(Vector3.Dot(-hit.normal, transform.forward));

                    // Trigger subsurface optical halo inside stone!
                    rock.InspectWithLight(hit.point, hit.normal, spotLight.color, spotLight.intensity, currentMode);

                    // Dynamic beam contraction when pressed against crust
                    spotLight.innerSpotAngle = Mathf.Lerp(8f, spotLight.spotAngle * 0.85f, proximity * alignment);
                }
            }
        }

        private void OnGUI()
        {
            if (spotLight == null || !spotLight.enabled || inspectedRock == null) return;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 13;
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.padding = new RectOffset(12, 12, 8, 8);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 14;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;

            GUIStyle detailStyle = new GUIStyle(GUI.skin.label);
            detailStyle.fontSize = 12;
            detailStyle.normal.textColor = new Color(0.85f, 0.92f, 0.85f);

            GUIStyle warningStyle = new GUIStyle(GUI.skin.label);
            warningStyle.fontSize = 13;
            warningStyle.fontStyle = FontStyle.Bold;
            warningStyle.normal.textColor = new Color(1.0f, 0.45f, 0.25f);

            float width = 440f;
            float height = 110f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.65f, width, height);

            GUI.Box(rect, GUIContent.none, boxStyle);

            GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 6, rect.width - 20, rect.height - 12));

            string modeName = currentMode == 0 ? "Warm Yellow (3000K)" : currentMode == 1 ? "Cool White (6500K)" : "UV Spectrum (365nm)";
            string stoneTitle = inspectedRock.Data != null ? inspectedRock.Data.rockName : "Raw Boulder";
            string priceTag = inspectedRock.IsMarketDisplay ? $" [Market Price: ${inspectedRock.MarketPrice}]" : "";

            GUILayout.Label($"🔦 <b>Torch Inspection</b>: {stoneTitle} ({inspectedRock.WeightKg:F1}kg){priceTag}", titleStyle);

            if (isInspectingTapeRock)
            {
                GUILayout.Label("🔒 <b>Opaque Industrial Tape</b>: Light cannot penetrate crust!", warningStyle);
                GUILayout.Label("<i>Shrewd merchant wrapped this boulder. 100% blind gamble!</i>", detailStyle);
            }
            else
            {
                JadeQuality q = inspectedRock.Quality;
                switch (currentMode)
                {
                    case 0: // Warm Yellow - Translucency & Core Hue
                        string waterLevel = q.translucency > 0.8f ? "Glass Species (~2.8cm penetration, crystal clear!)" :
                                            q.translucency > 0.45f ? "Ice Species (~1.5cm penetration, bright glow)" :
                                            q.translucency > 0.2f ? "Fine Sand (~0.6cm shallow light)" : "Dry Stone (Opaque, <0.2cm light)";

                        string hue = q.rarity == JadeRarity.ImperialGlass ? "Glowing Emerald Green (Legendary!)" :
                                     q.rarity == JadeRarity.AppleGreen ? "Fresh Apple Green" :
                                     q.rarity == JadeRarity.Lavender ? "Soft Violet / Lavender" :
                                     q.rarity == JadeRarity.BeanGreen ? "Muted Pea Green" : "Dull Chalky Grey";

                        GUILayout.Label($"<b>Water Level</b>: {waterLevel}", detailStyle);
                        GUILayout.Label($"<b>Optical Core Tint</b>: <color=#{ColorUtility.ToHtmlStringRGB(q.primaryColor)}>{hue}</color>", detailStyle);
                        break;

                    case 1: // Cool White - Surface Grain & Fissures
                        string grain = q.purity > 0.7f ? "Fine Crystalline Sand (Uniform, tight crystal grain)" :
                                       q.purity > 0.4f ? "Medium Grain (Some cotton flecks visible)" : "Coarse Grain (Rough inclusions detected)";
                        string cracks = q.crackSeverity > 0.4f ? "⚠️ High Risk: Noticeable surface cracks detected!" : "Clean Crust: Low surface fissure risk";

                        GUILayout.Label($"<b>Crust Texture</b>: {grain}", detailStyle);
                        GUILayout.Label(cracks, q.crackSeverity > 0.4f ? warningStyle : detailStyle);
                        break;

                    case 2: // UV 365nm - Authenticity
                        GUILayout.Label("<b>UV Reaction</b>: Inert / Natural (Type-A Untreated Jadeite)", detailStyle);
                        GUILayout.Label("<b>Chemical Scan</b>: No fluorescent polymer or glue injection detected.", detailStyle);
                        break;
                }

                GUILayout.Label("<i>[T] Switch Torch Mode (Warm / White / UV) | [F] Toggle Torch</i>", GUI.skin.label);
            }

            GUILayout.EndArea();
        }
    }
}
