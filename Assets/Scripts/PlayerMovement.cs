using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
    public CameraController _cameraController;
    [SerializeField] private GameObject _npcInRange;

    [Header("Skybox Settings")]
    [SerializeField] private Light directionalLight;  // Drag your Directional Light here
    [SerializeField] private Volume globalVolume;
    [SerializeField] private Material _daySkybox;
    [SerializeField] private Material _nightSkybox;
    private Color dayColor;
    private Color nightColor;
    private WhiteBalance whiteBalance;
    private ColorAdjustments colorAdjustments;

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

    private bool jumpQueued;

    private void Awake()
    {
        // Convert hex to Unity Color
        ColorUtility.TryParseHtmlString("#FFF7CB", out dayColor);   // soft yellow daylight
        ColorUtility.TryParseHtmlString("#CBD4FF", out nightColor); // soft bluish night
    }

    void Start()
    {
        _rb.drag = 0f;

        if (globalVolume != null && globalVolume.profile != null)
        {
            // Color Adjustments
            if (globalVolume.profile.TryGet(out ColorAdjustments adjustments))
            {
                colorAdjustments = adjustments;
            }
            else
            {
                Debug.LogWarning("No Color Adjustments override found on the Volume!");
            }

            // White Balance
            if (globalVolume.profile.TryGet(out WhiteBalance wb))
            {
                whiteBalance = wb;
            }
            else
            {
                Debug.LogWarning("No White Balance override found on the Volume!");
            }
        }

        ApplyDaySettings();
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

        // Toggle Sad Mode (Night/Day switch)
        if (Input.GetKeyDown(sadKey))
        {
            isSad = !isSad;
            animator.SetBool("IsSad", isSad);

            if (isSad)
            {
                ApplyNightSettings();
            }
            else
            {
                ApplyDaySettings();
            }
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

        if (isFlying || isFalling)
            targetSpeed *= 2f;

        if (isSad)
            targetSpeed /= 2;

        Vector3 move = moveDirection * targetSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + move);
    }

    private void DoJump()
    {
        readyToJump = false;
        isJumping = true;

        // reset vertical velocity so small impulses always work
        _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);

        float appliedJumpForce = jumpForce;

        // reduce jump if already airborne
        if (isFlying || isFalling)
            appliedJumpForce *= 0.5f; // 50% power, tweak as needed

        _rb.AddForce(Vector3.up * appliedJumpForce, ForceMode.Impulse);

        animator.SetTrigger("Jump");
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private void Interact()
    {
        isInteracting = true;
        animator.SetTrigger("Interact");

        if (_npcInRange != null)
        {
            // Rotate NPC to face player
            Vector3 npcDirection = (transform.position - _npcInRange.transform.position).normalized;
            npcDirection.y = 0f; // keep only horizontal rotation
            if (npcDirection != Vector3.zero)
            {
                Quaternion npcLookRotation = Quaternion.LookRotation(npcDirection);
                _npcInRange.transform.rotation = npcLookRotation;
            }

            // Rotate Player to face NPC
            Vector3 playerDirection = (_npcInRange.transform.position - transform.position).normalized;
            playerDirection.y = 0f; // keep only horizontal rotation
            if (playerDirection != Vector3.zero)
            {
                Quaternion playerLookRotation = Quaternion.LookRotation(playerDirection);
                transform.rotation = playerLookRotation;
            }

            // Cut Scene
            _cameraController.InteractCutScene(_npcInRange);
        }
    }

    private void FlyUpward()
    {
        Vector3 velocity = _rb.velocity;
        velocity.y = flyForce;
        _rb.velocity = velocity;

        animator.SetBool("IsFlying", true);
    }

    private void ApplyGlide()
    {
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
        Vector3 spherePosition = transform.position + Vector3.down * (playerHeight * 0.5f);
        isGrounded = Physics.CheckSphere(spherePosition, sphereRadius, whatIsGround);

        if (isGrounded && isJumping)
        {
            isJumping = false;
        }

        if (isGrounded)
        {
            isFlying = false;
            _rb.useGravity = true;
        }

        isFalling = !isGrounded && !isFlying;
    }

    private void RotatePlayerModel()
    {
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            playerModel.rotation = targetRotation;
        }
    }

    private void UpdateAnimator()
    {
        float targetSpeed = moveDirection.magnitude * (Input.GetKey(runKey) ? runSpeed : walkSpeed);

        if (isFlying || isFalling)
            targetSpeed *= 2f;

        animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsFalling", isFalling);
        animator.SetBool("IsFlying", isFlying);
        animator.SetBool("IsMoving", isMoving);
    }

    private void OnDrawGizmos()
    {
        Vector3 spherePosition = transform.position + Vector3.down * (playerHeight * 0.5f);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(spherePosition, sphereRadius);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("NPC"))
        {
            _npcInRange = other.gameObject;
            Debug.Log("Entered NPC range: " + other.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("NPC") && _npcInRange == other.gameObject)
        {
            _npcInRange = null;
            Debug.Log("Exited NPC range: " + other.name);
        }
    }

    // === Helper Methods for Day/Night ===
    private void ApplyDaySettings()
    {
        directionalLight.color = dayColor;
        directionalLight.intensity = 1f;

        colorAdjustments.hueShift.value = 0f;
        colorAdjustments.saturation.value = 0f;

        whiteBalance.temperature.value = -5;
        whiteBalance.tint.value = -30;

        RenderSettings.skybox = _daySkybox;
        DynamicGI.UpdateEnvironment();
    }

    private void ApplyNightSettings()
    {
        directionalLight.color = nightColor;
        directionalLight.intensity = 1.6f;

        colorAdjustments.hueShift.value = -6f;
        colorAdjustments.saturation.value = 12f;

        whiteBalance.temperature.value = -52f;
        whiteBalance.tint.value = -11f;

        RenderSettings.skybox = _nightSkybox;
        DynamicGI.UpdateEnvironment();
    }
}