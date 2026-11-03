using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    #region Logic
    [SerializeField] private LayerMask solidsLayer;
    private Rigidbody2D rigidBody;
    private BoxCollider2D collider;
    private GameInputs inputs;

    private float targetPlayerSpeed = 8.0f;
    private float jumpPower = 16.0f;
    private float xVelocity = 0.0f;
    private float accelTime = 0.1f;

    private float horizontalInput;
    private bool hasJumped = false;
    private bool isGrounded = false;
    private bool jumpCutOff = false;
    private bool jumpBufferActivated = false;

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

        float raylength = 0.01f;
        RaycastHit2D hit = Physics2D.BoxCast(collider.bounds.center, new Vector2(2.0f * collider.bounds.extents.x, 0.1f), 0.0f, Vector2.down, raylength + collider.bounds.extents.y, solidsLayer);
        Debug.DrawRay(collider.bounds.center, Vector2.down * (raylength + collider.bounds.extents.y), Color.red);

        if (hit.collider != null && rigidBody.linearVelocityY <= 0.05f)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        if (!hasJumped)
        {
            hasJumped = inputs.Player.Jump.WasPressedThisFrame();
        }

        if (!jumpCutOff && !isGrounded)
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
            hasJumped = false;
            if (isGrounded && !jumpBufferActivated)
            {
                Jump();
            }
            else if (!jumpBufferActivated)
            {
                jumpBufferActivated = true;
                StartCoroutine(JumpBuffer());
            }
        }

        if (jumpCutOff)
        {
            rigidBody.linearVelocityY *= 0.6f;
            jumpCutOff = false;
        }
    }

    private void Jump()
    {
        rigidBody.AddForceY(jumpPower, ForceMode2D.Impulse);
    }

    private IEnumerator JumpBuffer()
    {
        float jumpBufferTimer = 0.0f;
        float jumpBufferDuration = 0.1f;

        while (jumpBufferTimer < jumpBufferDuration)
        {
            if (isGrounded)
            {
                Jump();
                jumpBufferActivated = false;
                yield break;
            }

            jumpBufferTimer += Time.deltaTime;
            yield return null;
        }

        jumpBufferActivated = false;
    }
    #endregion
}