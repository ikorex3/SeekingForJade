using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace SeekingForJade.Player
{
    public class PlayerVisuals : NetworkBehaviour
    {
        [SerializeField] private GameObject firstPersonHands;
        [SerializeField] private GameObject thirdPersonAvatar;

        public GameObject FirstPersonHands
        {
            get => firstPersonHands;
            set => firstPersonHands = value;
        }

        public GameObject ThirdPersonAvatar
        {
            get => thirdPersonAvatar;
            set => thirdPersonAvatar = value;
        }

        public override void OnNetworkSpawn()
        {
            ApplyVisibility();
        }

        private void Start()
        {
            // Fallback for single-player / editor testing before network start
            if (!IsSpawned)
            {
                ApplyLocalOwnerVisibility();
            }
        }

        private void ApplyVisibility()
        {
            if (IsOwner)
            {
                ApplyLocalOwnerVisibility();
            }
            else
            {
                ApplyRemoteVisibility();
            }
        }

        private void ApplyLocalOwnerVisibility()
        {
            if (firstPersonHands != null)
            {
                firstPersonHands.SetActive(true);
            }

            if (thirdPersonAvatar != null)
            {
                // Local player casts shadows onto tables and floor, but avatar body does not clip local camera
                Renderer[] renderers = thirdPersonAvatar.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        private void ApplyRemoteVisibility()
        {
            if (firstPersonHands != null)
            {
                firstPersonHands.SetActive(false);
            }

            if (thirdPersonAvatar != null)
            {
                Renderer[] renderers = thirdPersonAvatar.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.shadowCastingMode = ShadowCastingMode.On;
                    r.enabled = true;
                }
            }
        }
    }
}
