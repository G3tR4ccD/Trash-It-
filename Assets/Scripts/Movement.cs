using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    // start Serialized fields
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Look")]
    public float mouseSensitivity = 0.1f;
    public Camera camera;
    public float Pitch 
    { 
        get => xRotation; 
        set => xRotation = value; 
    }
    public float Yaw
    {
        get => transform.eulerAngles.y;
        set => transform.eulerAngles = new Vector3(0f, value, 0f);
    }

    [Header("Ground Check")]
    public Transform groundCheck;
    // end Serialized fields

    // start Runtime state
    private CharacterController controller;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalVelocity;
    private float xRotation = 0f;
    bool isSprinting = false;
    // end Runtime state

    // start Awake
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    // end Awake

    // start Update
    private void Update()
    {
        Move();
        Look();

        if (animator != null)
        {
            Vector3 flat = controller.velocity;
            flat.y = 0f;
            animator.SetFloat("Speed", flat.magnitude, 0.1f, Time.deltaTime);
        }
    }
    // end Update

    // start OnMove
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    // end OnMove

    // start OnLook
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    // end OnLook

    // start OnJump
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!controller.isGrounded) return;

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }
    // end OnJump

    // start OnSprint
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isSprinting = true;
        }
        else if (context.canceled)
        {
            isSprinting = false;
        }
    }
    // end OnSprint

    // start Look
    private void Look()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);

    }
    // end Look

    // start Move
    private void Move()
    {
        Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
        direction = Vector3.ClampMagnitude(direction, 1f);

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        float currentSpeed;
        if (isSprinting == true)
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = moveSpeed;
        }

        Vector3 motion = direction * currentSpeed;
        motion.y = verticalVelocity;

        controller.Move(motion * Time.deltaTime);
    }
    // end Move
}