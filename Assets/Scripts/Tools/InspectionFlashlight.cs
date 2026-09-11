using SeekingForJade.Jade;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SeekingForJade.Tools
{
    public class InspectionFlashlight : MonoBehaviour
    {
        [Header("Light Components")]
        [SerializeField] private Light spotLight;
        [SerializeField] private float maxInspectionDistance = 2.5f;

        [Header("Modes")]
        [SerializeField] private Color warmYellowLight = new Color(1.0f, 0.88f, 0.55f);
        [SerializeField] private Color coolWhiteLight = new Color(0.85f, 0.95f, 1.0f);
        [SerializeField] private Color uvPurpleLight = new Color(0.6f, 0.2f, 1.0f);
        [SerializeField] private int currentMode = 0; // 0 = warm, 1 = white, 2 = uv

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;

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
                    spotLight.spotAngle = 22f;
                    spotLight.intensity = 1800f;
                    break;
                case 1:
                    spotLight.color = coolWhiteLight;
                    spotLight.spotAngle = 30f;
                    spotLight.intensity = 2200f;
                    break;
                case 2:
                    spotLight.color = uvPurpleLight;
                    spotLight.spotAngle = 25f;
                    spotLight.intensity = 1500f;
                    break;
            }
        }

        private bool isInspectingTapeRock = false;

        private void InspectRaycast()
        {
            isInspectingTapeRock = false;
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxInspectionDistance))
            {
                ProceduralRock rock = hit.collider.GetComponentInParent<ProceduralRock>();
                if (rock != null)
                {
                    if (rock.IsTapeWrapped)
                    {
                        // Opaque packing tape blocks all optical light penetration!
                        isInspectingTapeRock = true;
                        spotLight.innerSpotAngle = 5f; // Harsh sharp pinpoint reflection off tape
                        return;
                    }

                    // Closer inspection allows light penetration into translucent jade
                    float proximity = 1.0f - Mathf.Clamp01(hit.distance / maxInspectionDistance);
                    float alignment = Mathf.Clamp01(Vector3.Dot(-hit.normal, transform.forward));

                    // Boost spot focus on direct contact to reveal internal jade
                    spotLight.innerSpotAngle = Mathf.Lerp(5f, spotLight.spotAngle * 0.8f, proximity * alignment);
                }
            }
        }

        private void OnGUI()
        {
            if (isInspectingTapeRock && spotLight != null && spotLight.enabled)
            {
                GUIStyle style = new GUIStyle(GUI.skin.box);
                style.fontSize = 14;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = new Color(1.0f, 0.85f, 0.2f);
                style.alignment = TextAnchor.MiddleCenter;

                float width = 360f;
                float height = 32f;
                Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.65f, width, height);
                GUI.Box(rect, "🔒 Opaque Tape: Flashlight inspection blocked!", style);
            }
        }
    }
}
