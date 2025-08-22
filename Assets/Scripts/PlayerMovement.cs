using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private Rigidbody _rb;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float jumpForce = 4f;
    public float flyForce = 6f;
    public float glideFallSpeed = -2f;
    public float jumpCooldown = 0.25f;
    public float interactCooldown = 1f;
    public float groundDrag = 5f;

    [Header("Input Settings")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode interactKey = KeyCode.F;

    [Header("Ground Detection")]
    public float playerHeight = 0.2f;
    public float sphereRadius = 0.1f;
    public LayerMask whatIsGround;

    [Header("References")]
    public Camera mainCamera;
    public Animator animator;
    public Transform playerModel;

    private bool readyToJump = true;
    private bool isGrounded;
    private bool isJumping;
    private bool isFlying;
    private bool isInteracting;

    private Vector3 moveDirection;
    private float horizontalInput;
    private float verticalInput;

    void Update()
    {
        HandleInput();
        CheckGround();
        RotatePlayerModel();
        UpdateAnimator();
        ApplyDrag();
    }

    void FixedUpdate()
    {
        if (!isInteracting)
        {
            MovePlayer();
        }

        if (isFlying)
        {
            FlyUpward();
        }
        else if (!isGrounded) // apply glide
        {
            ApplyGlide();
        }
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Jump when on the ground
        if (Input.GetKeyDown(jumpKey) && readyToJump && isGrounded && !isInteracting)
        {
            Jump();
        }

        // Enter flying mode when space is held in the air
        if (Input.GetKey(jumpKey) && !isGrounded && !isInteracting)
        {
            isFlying = true;
            _rb.useGravity = false; // disable gravity while flying
        }

        // Stop flying when key is released
        if (Input.GetKeyUp(jumpKey) && !isInteracting)
        {
            isFlying = false;
            _rb.useGravity = true; // re-enable gravity when flight ends
        }

        // Interact when on ground and is not falling nor flying
        if (Input.GetKeyDown(interactKey) && isGrounded && !isFlying && !isInteracting)
        {
            Interact();
        }
    }

    private void MovePlayer()
    {
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        moveDirection = (camForward * verticalInput + camRight * horizontalInput).normalized;

        float targetSpeed = Input.GetKey(runKey) ? runSpeed : walkSpeed;

        // Preserve Y velocity (gravity / flying handles vertical movement)
        Vector3 horizontalVelocity = moveDirection * targetSpeed;
        Vector3 velocity = new(horizontalVelocity.x, _rb.velocity.y, horizontalVelocity.z);
        _rb.velocity = velocity;
    }

    private void Jump()
    {
        readyToJump = false;

        // Reset vertical velocity
        _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);

        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        animator.SetTrigger("Jump"); // one-shot jump anim

        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private void Interact()
    {
        isInteracting = true;

        animator.SetTrigger("Interact");

        Invoke(nameof(ResetInteract), interactCooldown);
    }

    private void FlyUpward()
    {
        // Instead of adding force, directly control vertical velocity
        Vector3 velocity = _rb.velocity;
        velocity.y = flyForce;
        _rb.velocity = velocity;

        animator.SetBool("IsFlying", true);
    }

    private void ApplyGlide()
    {
        // Limit fall speed to glideFallSpeed (less negative = slower fall)
        if (_rb.velocity.y < glideFallSpeed)
        {
            Vector3 velocity = _rb.velocity;
            velocity.y = glideFallSpeed;
            _rb.velocity = velocity;
        }
    }

    private void ResetJump() => readyToJump = true;
    private void ResetInteract() => isInteracting = false;

    private void CheckGround()
    {
        Vector3 spherePosition = transform.position + Vector3.down * (playerHeight * 0.5f - sphereRadius);

        isGrounded = Physics.CheckSphere(spherePosition, sphereRadius, whatIsGround);

        if (isGrounded && isJumping)
        {
            isJumping = false;
        }

        // Exit flying state when grounded
        if (isGrounded)
        {
            isFlying = false;
            _rb.useGravity = true; // restore gravity
        }
    }

    private void ApplyDrag()
    {
        _rb.drag = isGrounded ? groundDrag : 0f;
    }

    private void RotatePlayerModel()
    {
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void UpdateAnimator()
    {
        float targetSpeed = moveDirection.magnitude * (Input.GetKey(runKey) ? runSpeed : walkSpeed);
        animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsFalling", !isGrounded && _rb.velocity.y < -0.1f && !isFlying);
        animator.SetBool("IsFlying", isFlying);
    }

    private void OnDrawGizmos()
    {
        Vector3 spherePosition = transform.position + Vector3.down * (playerHeight * 0.5f - sphereRadius);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(spherePosition, sphereRadius);
    }
}
