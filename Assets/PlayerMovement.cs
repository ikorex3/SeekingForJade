using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private Transform playerCamera;

    private CharacterController controller;
    private float xRotation = 0f;

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
            float x = 0f;
            float z = 0f;

            if (keyboard.wKey.isPressed) z += 1f;
            if (keyboard.sKey.isPressed) z -= 1f;
            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;

            Vector3 direction = (transform.forward * z + transform.right * x).normalized;

            if (direction.magnitude >= 0.1f)
            {
                controller.Move(direction * speed * Time.deltaTime);
            }

            if (!controller.isGrounded)
            {
                controller.Move(Vector3.down * 9.81f * Time.deltaTime);
            }
        }
    }
}