using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8.5f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -19.62f;

    [Header("Look & Physics")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float pushPower = 2.0f;
    [SerializeField] private Transform playerCamera;

    private CharacterController controller;
    private float xRotation = 0f;
    private Vector3 verticalVelocity;
    private float jumpCooldownTimer = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false);
            }
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue() * mouseSensitivity;

            transform.Rotate(0f, mouseDelta.x, 0f);

            xRotation -= mouseDelta.y;
            xRotation = Mathf.Clamp(xRotation, -85f, 85f);

            if (playerCamera != null)
            {
                playerCamera.localEulerAngles = new Vector3(xRotation, 0f, 0f);
            }
        }

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            bool isGrounded = controller.isGrounded;

            if (isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }

            bool isSprinting = keyboard.leftShiftKey.isPressed;
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            float x = 0f;
            float z = 0f;
            if (keyboard.wKey.isPressed) z += 1f;
            if (keyboard.sKey.isPressed) z -= 1f;
            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;

            Vector3 moveDirection = (transform.forward * z + transform.right * x).normalized;
            if (keyboard.spaceKey.wasPressedThisFrame && isGrounded && jumpCooldownTimer <= 0f)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpCooldownTimer = 0.2f;
            }

            verticalVelocity.y += gravity * Time.deltaTime;

            Vector3 motion = (moveDirection * currentSpeed + verticalVelocity) * Time.deltaTime;
            controller.Move(motion);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!IsOwner) return;

        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;
        if (hit.moveDirection.y < -0.3f) return;

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        Vector3 force = pushDir * pushPower;
        NetworkObject netObj = hit.collider.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            PushServerRpc(netObj.NetworkObjectId, force);
        }
        else
        {
            body.linearVelocity = force;
        }
    }

    [ServerRpc]
    private void PushServerRpc(ulong networkObjectId, Vector3 force)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = force;
            }
        }
    }
}