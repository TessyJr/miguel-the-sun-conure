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

    [Header("Input Settings")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode interactKey = KeyCode.F;
    public KeyCode sadKey = KeyCode.P;

    [Header("Ground Detection")]
    public float playerHeight = 1.8f;
    public float sphereRadius = 0.25f;
    public LayerMask whatIsGround;

    [Header("References")]
    public Camera mainCamera;
    public Animator animator;
    public Transform playerModel;

    [Header("Status")]
    [SerializeField] private bool isSad = false;
    [SerializeField] private bool readyToJump = true;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isJumping;
    [SerializeField] private bool isFlying;
    [SerializeField] private bool isFalling;
    [SerializeField] private bool isInteracting;
    [SerializeField] private bool isMoving;

    private Vector3 moveDirection;
    private float horizontalInput;
    private float verticalInput;

    // queued jump flag (Update → FixedUpdate)
    private bool jumpQueued;

    void Start()
    {
        // keep drag disabled always
        _rb.drag = 0f;
    }

    void Update()
    {
        HandleInput();
        CheckGround();
        RotatePlayerModel();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        MovePlayer();

        if (jumpQueued)
        {
            DoJump();
            jumpQueued = false;
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

        // Check if player is moving
        isMoving = horizontalInput != 0 || verticalInput != 0;

        // Queue jump
        if (Input.GetKeyDown(jumpKey) && readyToJump && isGrounded && !isInteracting)
        {
            jumpQueued = true;
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
        if (Input.GetKeyDown(interactKey) && isGrounded && !isFlying && !isInteracting && !isMoving)
        {
            Interact();
        }

        if (Input.GetKeyDown(sadKey))
        {
            isSad = !isSad;
            animator.SetBool("IsSad", isSad);
        }
    }

    private void MovePlayer()
    {
        isInteracting = false;

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        moveDirection = (camForward * verticalInput + camRight * horizontalInput).normalized;

        float targetSpeed = Input.GetKey(runKey) && !isSad ? runSpeed : walkSpeed;

        // Double speed if flying or falling
        if (isFlying || isFalling)
            targetSpeed *= 2f;

        if (isSad)
            targetSpeed /= 2;

        // Preserve Y velocity (gravity / flying handles vertical movement)
        Vector3 horizontalVelocity = moveDirection * targetSpeed;
        Vector3 velocity = new(horizontalVelocity.x, _rb.velocity.y, horizontalVelocity.z);
        _rb.velocity = velocity;
    }

    private void DoJump()
    {
        readyToJump = false;
        isJumping = true;

        // reset vertical velocity so small impulses always work
        _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        animator.SetTrigger("Jump");
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private void Interact()
    {
        isInteracting = true;
        animator.SetTrigger("Interact");
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

    private void CheckGround()
    {
        // ground check positioned lower + bigger radius
        Vector3 spherePosition = transform.position + Vector3.down * (playerHeight * 0.5f);
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

        // Falling check (only when not grounded and not flying)
        isFalling = !isGrounded && !isFlying;
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

        // Match animator speed boost too
        if (isFlying || isFalling)
            targetSpeed *= 2f;

        animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsFalling", isFalling);
        animator.SetBool("IsFlying", isFlying);

        // Send isMoving to animator
        animator.SetBool("IsMoving", isMoving);
    }

    private void OnDrawGizmos()
    {
        Vector3 spherePosition = transform.position + Vector3.down * (playerHeight * 0.5f);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(spherePosition, sphereRadius);
    }
}
