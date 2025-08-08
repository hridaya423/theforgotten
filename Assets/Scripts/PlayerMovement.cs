using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Parameters")]
    [Tooltip("Basic movement speed")]
    public float moveSpeed = 6f;
    [Tooltip("Sprint multiplier")]
    public float sprintMultiplier = 1.5f;

    [Header("Gravity and Jumping")]
    [Tooltip("Gravity force applied to the player")]
    public float gravity = -19.6f;
    [Tooltip("Initial jump velocity")]
    public float jumpHeight = 2f;
    [Tooltip("Additional gravity multiplier for more responsive falling")]  
    public float fallMultiplier = 2.5f;

    [Header("Ground Detection")]
    [Tooltip("Transform used to check if player is grounded")]
    public Transform groundCheck;
    [Tooltip("Radius of ground check sphere")]
    public float groundCheckRadius = 0.3f;
    [Tooltip("Layers considered as ground")]
    public LayerMask groundMask;

    [Header("Advanced Movement")]
    [Tooltip("How quickly the player accelerates")]
    public float acceleration = 10f;
    [Tooltip("How quickly the player decelerates")]
    public float deceleration = 10f;

    
    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isMoving;

    void Start()
    {
        
        controller = GetComponent<CharacterController>();

        
        if (groundCheck == null)
        {
            Debug.LogWarning("Ground check transform is not set. Creating a default ground check point.");
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -controller.height / 2, 0);
            groundCheck = groundCheckObj.transform;
        }
    }

    void Update()
    {
        
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        
        Vector3 desiredMoveDirection = transform.right * moveHorizontal + transform.forward * moveVertical;
        desiredMoveDirection = Vector3.ClampMagnitude(desiredMoveDirection, 1f);

        
        float currentMoveSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * sprintMultiplier : moveSpeed;

        
        moveDirection = Vector3.Lerp(moveDirection, desiredMoveDirection * currentMoveSpeed,
            Time.deltaTime * (desiredMoveDirection.magnitude > 0.1f ? acceleration : deceleration));

        
        controller.Move(moveDirection * Time.deltaTime);

        
        HandleJumping();

        
        ApplyGravity();

        
        UpdateMovementState();
    }

    void HandleJumping()
    {
        
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void ApplyGravity()
    {
        
        velocity.y += gravity * Time.deltaTime;

        
        if (velocity.y < 0)
        {
            velocity.y += gravity * (fallMultiplier - 1) * Time.deltaTime;
        }

        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateMovementState()
    {
        
        isMoving = controller.velocity.magnitude > 0.1f && isGrounded;
    }

    
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
    