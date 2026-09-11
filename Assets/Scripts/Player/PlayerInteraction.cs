using SeekingForJade.Economy;
using SeekingForJade.Environment;
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

        [Header("Network State")]
        private readonly NetworkVariable<NetworkObjectReference> netCarriedRock = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly NetworkVariable<float> netCameraPitch = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

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

        public override void OnNetworkSpawn()
        {
            netCarriedRock.OnValueChanged += OnCarriedRockChanged;

            if (netCarriedRock.Value.TryGet(out NetworkObject rockNetObj))
            {
                OnCarriedRockChanged(default, netCarriedRock.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            netCarriedRock.OnValueChanged -= OnCarriedRockChanged;
        }

        private void OnCarriedRockChanged(NetworkObjectReference oldRef, NetworkObjectReference newRef)
        {
            if (newRef.TryGet(out NetworkObject newRockObj))
            {
                carriedRock = newRockObj.GetComponent<ProceduralRock>();
                if (carriedRock != null)
                {
                    // Disable collider while carried so player movement is never blocked!
                    if (carriedRock.TryGetComponent<Collider>(out var col))
                    {
                        col.enabled = false;
                    }
                    if (carriedRock.TryGetComponent<Rigidbody>(out var rb))
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.isKinematic = true;
                    }
                    if (carriedRock.TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var netTransform))
                    {
                        netTransform.enabled = false;
                    }
                }
            }
            else
            {
                if (carriedRock != null)
                {
                    if (carriedRock.TryGetComponent<Collider>(out var col))
                    {
                        col.enabled = true;
                    }
                    if (carriedRock.TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var netTransform))
                    {
                        netTransform.enabled = true;
                    }
                    carriedRock = null;
                }
            }
        }

        private void Update()
        {
            if (IsSpawned && !IsOwner)
            {
                // Synchronize remote player's camera pitch so head/arms tilt
                if (playerCamera != null)
                {
                    playerCamera.localEulerAngles = new Vector3(netCameraPitch.Value, 0f, 0f);
                }
                return;
            }

            if (playerCamera != null && IsSpawned && IsOwner)
            {
                float pitch = playerCamera.localEulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
                netCameraPitch.Value = pitch;
            }

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
                if (hitSomething)
                {
                    CuttingSawStation saw = hit.collider.GetComponentInParent<CuttingSawStation>();
                    if (saw != null && saw.State == SawState.Idle)
                    {
                        currentPrompt = "[E] Clamp Rock into Saw";
                        if (interactPressed)
                        {
                            ClampRockToSaw(saw);
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
                    MiningPile mine = hit.collider.GetComponentInParent<MiningPile>();
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
                    JadeTraderNPC trader = hit.collider.GetComponentInParent<JadeTraderNPC>();
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
                                JadeTraderNPC traderNpc = Object.FindAnyObjectByType<JadeTraderNPC>();
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

        private void LateUpdate()
        {
            if (holdPoint == null) return;

            // 1. Local owner position update for zero lag
            if (carriedRock != null)
            {
                carriedRock.transform.position = holdPoint.position;
                carriedRock.transform.rotation = holdPoint.rotation;
            }

            // 2. Server updates rock position on network so NetworkTransform replicates it
            if (IsServer && netCarriedRock.Value.TryGet(out NetworkObject rockNetObj))
            {
                rockNetObj.transform.position = holdPoint.position;
                rockNetObj.transform.rotation = holdPoint.rotation;
            }
        }

        private void PickUpRock(ProceduralRock rock)
        {
            if (rock == null) return;

            carriedRock = rock;

            // Disable collider immediately so it never blocks the player's CharacterController
            if (rock.TryGetComponent<Collider>(out var col))
            {
                col.enabled = false;
            }

            if (rock.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            if (rock.TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var netTransform))
            {
                netTransform.enabled = false;
            }

            if (IsSpawned)
            {
                if (rock.TryGetComponent<NetworkObject>(out var rockNetObj))
                {
                    RequestPickUpServerRpc(rockNetObj);
                }
            }
        }

        private void ThrowRock()
        {
            if (carriedRock == null) return;

            ProceduralRock rock = carriedRock;
            carriedRock = null;

            Vector3 throwVelocity = playerCamera != null ? playerCamera.forward * throwForce : transform.forward * throwForce;

            if (IsSpawned)
            {
                RequestThrowServerRpc(throwVelocity);
            }
            else
            {
                if (rock.TryGetComponent<Collider>(out var col))
                {
                    col.enabled = true;
                }
                if (rock.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = false;
                    rb.linearVelocity = throwVelocity;
                    rb.angularVelocity = Random.insideUnitSphere * 6f;
                }

                if (rock.TryGetComponent<RockImpactBreaker>(out var breaker))
                {
                    breaker.IsArmed = true;
                }
            }
        }

        private void DropRock()
        {
            if (carriedRock == null) return;

            ProceduralRock rock = carriedRock;
            carriedRock = null;

            Vector3 dropVelocity = playerCamera != null ? playerCamera.forward * 1.5f : transform.forward * 1.5f;

            if (IsSpawned)
            {
                RequestDropServerRpc(dropVelocity);
            }
            else
            {
                if (rock.TryGetComponent<Collider>(out var col))
                {
                    col.enabled = true;
                }
                if (rock.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = false;
                    rb.linearVelocity = dropVelocity;
                }

                if (rock.TryGetComponent<RockImpactBreaker>(out var breaker))
                {
                    breaker.IsArmed = false;
                }
            }
        }

        private void ClampRockToSaw(CuttingSawStation saw)
        {
            ProceduralRock rockToClamp = carriedRock;
            carriedRock = null;

            if (IsSpawned)
            {
                RequestClampRockToSawServerRpc();
            }

            saw.TryLoadRock(rockToClamp);
        }

        [ServerRpc]
        private void RequestPickUpServerRpc(NetworkObjectReference rockRef)
        {
            if (!rockRef.TryGet(out NetworkObject rockNetObj)) return;

            if (Vector3.Distance(transform.position, rockNetObj.transform.position) > reachDistance + 2.0f)
            {
                return;
            }

            netCarriedRock.Value = rockRef;

            if (rockNetObj.TryGetComponent<ProceduralRock>(out var rock))
            {
                if (rock.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }

                SetRockColliderClientRpc(rockRef, false);
            }
        }

        [ServerRpc]
        private void RequestThrowServerRpc(Vector3 throwVelocity)
        {
            if (netCarriedRock.Value.TryGet(out NetworkObject rockNetObj))
            {
                netCarriedRock.Value = default;

                SetRockColliderClientRpc(rockNetObj, true);

                if (rockNetObj.TryGetComponent<ProceduralRock>(out var rock))
                {
                    if (rock.TryGetComponent<Rigidbody>(out var rb))
                    {
                        rb.isKinematic = false;
                        rb.linearVelocity = throwVelocity;
                        rb.angularVelocity = Random.insideUnitSphere * 6f;
                    }

                    if (rock.TryGetComponent<RockImpactBreaker>(out var breaker))
                    {
                        breaker.IsArmed = true;
                    }
                }
            }
        }

        [ServerRpc]
        private void RequestDropServerRpc(Vector3 dropVelocity)
        {
            if (netCarriedRock.Value.TryGet(out NetworkObject rockNetObj))
            {
                netCarriedRock.Value = default;

                SetRockColliderClientRpc(rockNetObj, true);

                if (rockNetObj.TryGetComponent<ProceduralRock>(out var rock))
                {
                    if (rock.TryGetComponent<Rigidbody>(out var rb))
                    {
                        rb.isKinematic = false;
                        rb.linearVelocity = dropVelocity;
                    }

                    if (rock.TryGetComponent<RockImpactBreaker>(out var breaker))
                    {
                        breaker.IsArmed = false;
                    }
                }
            }
        }

        [ServerRpc]
        private void RequestClampRockToSawServerRpc()
        {
            if (netCarriedRock.Value.TryGet(out NetworkObject rockNetObj))
            {
                netCarriedRock.Value = default;
                SetRockColliderClientRpc(rockNetObj, true);
            }
        }

        [ClientRpc]
        private void SetRockColliderClientRpc(NetworkObjectReference rockRef, bool colliderEnabled)
        {
            if (rockRef.TryGet(out NetworkObject rockNetObj))
            {
                if (rockNetObj.TryGetComponent<Collider>(out var col))
                {
                    col.enabled = colliderEnabled;
                }
                if (rockNetObj.TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var netTransform))
                {
                    netTransform.enabled = colliderEnabled;
                }
                if (rockNetObj.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = !colliderEnabled;
                    if (!colliderEnabled)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
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
