using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    private Rigidbody2D rigidBody;
    private BoxCollider2D collider;
    private GameInputs inputs;

    private float targetPlayerSpeed = 8.0f;
    private float jumpPower = 16.0f;
    private float xVelocity = 0.0f;
    private float accelTime = 0.1f;

    private float horizontalInput;
    private bool hasJumped = false;
    private bool canJump = false;
    private bool jumpCutOff = false;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        collider = GetComponent<BoxCollider2D>();
        inputs = new GameInputs();
    }

    private void OnEnable()
    {
        inputs.Enable();
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        horizontalInput = inputs.Player.HorizontalMovement.ReadValue<float>();

        float raylength = 0.1f;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(collider.bounds.center.x, collider.bounds.center.y - collider.bounds.extents.y), Vector2.down, raylength, groundLayer);
        Debug.DrawRay(new Vector2(collider.bounds.center.x, collider.bounds.center.y - collider.bounds.extents.y), Vector2.down * raylength, Color.red);

        if (hit.collider != null && rigidBody.linearVelocityY <= 0.05f)
        {
            canJump = true;
        } else
        {
            canJump = false;
        }

        if (!hasJumped && canJump)
        {
            hasJumped = inputs.Player.Jump.WasPressedThisFrame();
        }

        if (!jumpCutOff && !canJump)
        {
            jumpCutOff = (inputs.Player.Jump.WasReleasedThisFrame() && rigidBody.linearVelocityY > 0.0f);
        }
    }

    private void FixedUpdate()
    {
        xVelocity = rigidBody.linearVelocityX;
        float targetXVelocity = horizontalInput * targetPlayerSpeed;
        xVelocity = Mathf.MoveTowards(xVelocity, targetXVelocity, targetPlayerSpeed / accelTime * Time.fixedDeltaTime);
        rigidBody.linearVelocityX = xVelocity;

        if (hasJumped)
        {
            rigidBody.AddForceY(jumpPower, ForceMode2D.Impulse);
            hasJumped = false;
        }

        if (jumpCutOff)
        {
            rigidBody.linearVelocityY *= 0.5f;
            jumpCutOff = false;
        }
    }
}
