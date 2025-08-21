using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float jumpForce = 4f;
    public float jumpCooldown = 0.25f;
    public float airMultiplier = 0.4f;
    public float groundDrag = 5f;

    [Header("Input Settings")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode runKey = KeyCode.LeftShift;

    [Header("Ground Detection")]
    public float playerHeight = 0.2f;
    public float sphereRadius = 0.1f;
    public LayerMask whatIsGround;

    [Header("References")]
    public Camera mainCamera;
    public Animator animator;
    public Transform playerModel;

    private Rigidbody rb;
    private bool readyToJump = true;
    private bool isGrounded;
    private bool isJumping;
    private Vector3 moveDirection;

    private float horizontalInput;
    private float verticalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

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
        MovePlayer();
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(jumpKey) && readyToJump && isGrounded)
        {
            Jump();
        }
    }

    private void MovePlayer()
    {
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f; camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        moveDirection = (camForward * verticalInput + camRight * horizontalInput).normalized;

        float targetSpeed = Input.GetKey(runKey) ? runSpeed : walkSpeed;

        if (isGrounded)
        {
            // Add horizontal velocity instantly
            Vector3 horizontalVelocity = moveDirection * targetSpeed;
            Vector3 velocity = new(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);
            rb.velocity = velocity;
        }
        else
        {
            // Air movement
            rb.AddForce(moveDirection * targetSpeed * airMultiplier, ForceMode.Force);
        }
    }

    private void Jump()
    {
        readyToJump = false;
        isJumping = true;

        // Reset vertical velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private void ResetJump() => readyToJump = true;

    private void CheckGround()
    {
        Vector3 spherePosition = transform.position + Vector3.down * (playerHeight * 0.5f - sphereRadius);

        isGrounded = Physics.CheckSphere(spherePosition, sphereRadius, whatIsGround);

        if (isGrounded && isJumping)
            isJumping = false;
    }

    private void ApplyDrag()
    {
        rb.drag = isGrounded ? groundDrag : 0f;
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
        if (!animator) return;

        float targetSpeed = moveDirection.magnitude * (Input.GetKey(runKey) ? runSpeed : walkSpeed);

        // Damped speed for smooth transitions
        animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsJumping", isJumping);
    }


    private void OnDrawGizmos()
    {
        Vector3 spherePosition = transform.position + Vector3.down * (playerHeight * 0.5f - sphereRadius);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(spherePosition, sphereRadius);
    }
}
