using SeekingForJade.Jade;
using SeekingForJade.Workstations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SeekingForJade.Player
{
    public class PlayerInteraction : NetworkBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float reachDistance = 3.2f;
        [SerializeField] private float throwForce = 9.5f;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private Transform playerCamera;

        [Header("State")]
        [SerializeField] private ProceduralRock carriedRock;
        [SerializeField] private string currentPrompt = "";

        private void Awake()
        {
            if (playerCamera == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) playerCamera = cam.transform;
            }

            if (holdPoint == null && playerCamera != null)
            {
                GameObject hp = new GameObject("HoldPoint");
                hp.transform.SetParent(playerCamera);
                hp.transform.localPosition = new Vector3(0.35f, -0.3f, 0.85f);
                holdPoint = hp.transform;
            }
        }

        private void Update()
        {
            if (IsSpawned && !IsOwner) return;

            currentPrompt = "";

            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, reachDistance);

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null) return;

            bool interactPressed = keyboard.eKey.wasPressedThisFrame;
            bool buyPressed = keyboard.bKey.wasPressedThisFrame;
            bool dropPressed = keyboard.qKey.wasPressedThisFrame || (mouse != null && mouse.rightButton.wasPressedThisFrame);

            // Handle Carried Rock
            if (carriedRock != null)
            {
                carriedRock.transform.position = holdPoint.position;
                carriedRock.transform.rotation = holdPoint.rotation;

                if (hitSomething)
                {
                    CuttingSawStation saw = hit.collider.GetComponentInParent<CuttingSawStation>();
                    if (saw != null && saw.State == SawState.Idle)
                    {
                        currentPrompt = "[E] Clamp Rock into Saw";
                        if (interactPressed)
                        {
                            ProceduralRock rockToClamp = carriedRock;
                            carriedRock = null;
                            saw.TryLoadRock(rockToClamp);
                            return;
                        }
                    }
                }

                if (string.IsNullOrEmpty(currentPrompt))
                {
                    currentPrompt = $"Holding: {carriedRock.WeightKg:F1}kg {carriedRock.Quality.rarity} | [L-Click] Throw & Smash | [Q] Drop";
                }

                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    ThrowRock();
                    return;
                }

                if (dropPressed)
                {
                    DropRock();
                    return;
                }
            }
            else
            {
                // Not carrying anything
                if (hitSomething)
                {
                    // Check Cutting Station
                    CuttingSawStation saw = hit.collider.GetComponentInParent<CuttingSawStation>();
                    if (saw != null)
                    {
                        if (saw.State == SawState.RockLoaded)
                        {
                            currentPrompt = "[E] Start Cutting Saw";
                            if (interactPressed)
                            {
                                saw.StartCut();
                                return;
                            }
                        }
                        else if (saw.State == SawState.CutComplete)
                        {
                            currentPrompt = "[E] Reset Saw Station";
                            if (interactPressed)
                            {
                                saw.ResetStation();
                                return;
                            }
                        }
                    }

                    // Check Mining Pile
                    SeekingForJade.Environment.MiningPile mine = hit.collider.GetComponentInParent<SeekingForJade.Environment.MiningPile>();
                    if (mine != null)
                    {
                        currentPrompt = mine.GetPromptText();
                        if (interactPressed && mine.IsReady)
                        {
                            if (mine.TryMineRock(out GameObject spawnedRock))
                            {
                                SeekingForJade.VFX.RockVFXManager.SpawnSmashImpactVFX(mine.transform.position + Vector3.up * 0.8f, Vector3.up);
                                PlayerVisuals vis = GetComponent<PlayerVisuals>();
                                if (vis != null) vis.TriggerMiningAnimation();
                            }
                            return;
                        }
                    }

                    // Check Trader NPC & Appraisal Scale
                    SeekingForJade.Economy.JadeTraderNPC trader = hit.collider.GetComponentInParent<SeekingForJade.Economy.JadeTraderNPC>();
                    if (trader != null)
                    {
                        bool lookingAtScale = hit.collider.name.Contains("Scale") || hit.collider.name.Contains("Plate");

                        if (lookingAtScale)
                        {
                            currentPrompt = trader.GetAppraisalPrompt();
                            if (interactPressed)
                            {
                                trader.TrySellPlacedSlices(out _, out _);
                                return;
                            }
                        }
                        else
                        {
                            // Looking at Master Chen or Trader Stall
                            currentPrompt = trader.GetGreetingPrompt();
                            if (interactPressed)
                            {
                                trader.SpeakNextAdvice();
                                return;
                            }
                            if (buyPressed)
                            {
                                if (trader.TryBuyFirstAvailable(out ProceduralRock boughtRock))
                                {
                                    PickUpRock(boughtRock);
                                }
                                return;
                            }
                        }
                    }

                    // Check Rock
                    ProceduralRock rock = hit.collider.GetComponentInParent<ProceduralRock>();
                    if (rock != null)
                    {
                        if (rock.IsMarketDisplay)
                        {
                            currentPrompt = $"[E] or [B] Buy {rock.Data?.rockName ?? "Boulder"} (${rock.MarketPrice}) | [F] Inspect with Torch";
                            if (interactPressed || buyPressed)
                            {
                                SeekingForJade.Economy.JadeTraderNPC traderNpc = Object.FindAnyObjectByType<SeekingForJade.Economy.JadeTraderNPC>();
                                if (traderNpc != null && traderNpc.TryBuyDisplayRock(rock))
                                {
                                    PickUpRock(rock);
                                }
                                return;
                            }
                        }
                        else
                        {
                            string valueText = rock.IsSliced ? $" (${rock.GetEstimatedValue():N0})" : "";
                            string methodText = rock.Method == CutMethod.RoughSmash ? " [Smashed -60%]" : (rock.Method == CutMethod.PrecisionSaw ? " [Clean Cut]" : "");
                            currentPrompt = $"[E] Pick Up {rock.WeightKg:F1}kg {rock.Quality.rarity}{methodText}{valueText}";

                            if (interactPressed)
                            {
                                PickUpRock(rock);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void PickUpRock(ProceduralRock rock)
        {
            carriedRock = rock;
            Rigidbody rb = rock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }
            rock.transform.SetParent(holdPoint);
        }

        private void ThrowRock()
        {
            if (carriedRock == null) return;

            ProceduralRock rock = carriedRock;
            carriedRock = null;

            rock.transform.SetParent(null);
            Rigidbody rb = rock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = playerCamera.forward * throwForce;
                rb.angularVelocity = Random.insideUnitSphere * 6f;
            }

            RockImpactBreaker breaker = rock.GetComponent<RockImpactBreaker>();
            if (breaker != null)
            {
                breaker.IsArmed = true;
            }
        }

        private void DropRock()
        {
            if (carriedRock == null) return;

            ProceduralRock rock = carriedRock;
            carriedRock = null;

            rock.transform.SetParent(null);
            Rigidbody rb = rock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = playerCamera.forward * 1.5f;
            }

            RockImpactBreaker breaker = rock.GetComponent<RockImpactBreaker>();
            if (breaker != null)
            {
                breaker.IsArmed = false;
            }
        }

        private void OnGUI()
        {
            if (IsSpawned && !IsOwner) return;

            // Draw simple center crosshair
            float cx = Screen.width / 2f;
            float cy = Screen.height / 2f;
            GUI.Box(new Rect(cx - 2, cy - 2, 4, 4), GUIContent.none);

            // Draw interaction prompt
            if (!string.IsNullOrEmpty(currentPrompt))
            {
                GUIStyle promptStyle = new GUIStyle(GUI.skin.box);
                promptStyle.fontSize = 18;
                promptStyle.fontStyle = FontStyle.Bold;
                promptStyle.normal.textColor = Color.white;
                promptStyle.alignment = TextAnchor.MiddleCenter;

                float width = 420;
                float height = 36;
                GUI.Box(new Rect(cx - width / 2f, cy + 30f, width, height), currentPrompt, promptStyle);
            }
        }
    }
}
