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

    private float xVelocity = 0.0f;
    private float yVelocity = 0.0f;

    private float horizontalInput = 0.0f;
    private float targetPlayerSpeed = 8.0f;
    private float accelTime = 0.1f;
    private float deccelTime = 0.05f;

    private float gravity = 0.6f;
    private float fallGravity = 0.8f;
    private bool isGrounded = false;
    private bool jumpFlag = false;
    private float jumpPower = 16.0f;
    private float castLength = 0.003f;

    private bool jumpCutOff = false;

    private bool jumpBufferActivated = false;

    private bool coyoteJumpActivated = false;
    private bool previousGroundCast = false;

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

    private void Update()
    {
        horizontalInput = inputs.Player.HorizontalMovement.ReadValue<float>();

        if (!jumpFlag)
        {
            jumpFlag = inputs.Player.Jump.WasPressedThisFrame();
        }

        if (!jumpCutOff && rigidBody.linearVelocityY > 0.0f)
        {
            jumpCutOff = inputs.Player.Jump.WasReleasedThisFrame();
        }

        Debug.DrawRay(transform.position, Vector2.down * (castLength + collider.bounds.extents.y), Color.red);
    }

    private void FixedUpdate()
    {
        GroundDetection();
        CeilingDetection();

        float targetXVelocity = horizontalInput * targetPlayerSpeed;
        if (targetXVelocity != 0.0f)
        {
            xVelocity = Mathf.MoveTowards(xVelocity, targetXVelocity, targetPlayerSpeed / accelTime * Time.fixedDeltaTime);
        } else
        {
            xVelocity = Mathf.MoveTowards(xVelocity, targetXVelocity, targetPlayerSpeed / deccelTime * Time.fixedDeltaTime);
        }

        if (jumpFlag)
        {
            jumpFlag = false;
            if (coyoteJumpActivated)
            {
                Jump();
                coyoteJumpActivated = false;
            } else if (!jumpBufferActivated)
            {
                if (isGrounded)
                {
                    Jump();
                }
                else
                {
                    jumpBufferActivated = true;
                    StartCoroutine(JumpBuffer());
                }
            }
        }

        if (jumpCutOff)
        {
            jumpCutOff = false;
            yVelocity *= 0.6f;
        }

        if (yVelocity <= 0.0f)
        {
            yVelocity -= fallGravity;
        } else
        {
            yVelocity -= gravity;
        }
        
        rigidBody.linearVelocity = new Vector2(xVelocity, yVelocity);
    }

    private void GroundDetection()
    {
        RaycastHit2D hit = Physics2D.BoxCast(rigidBody.position, new Vector2(2.0f * collider.bounds.extents.x, 0.1f), 0.0f, Vector2.down, castLength + collider.bounds.extents.y, solidsLayer);

        if (hit && yVelocity <= 0.0f)
        {
            isGrounded = true;
            yVelocity = 0.0f;
        } else
        {
            isGrounded = false;
        }

        if (previousGroundCast && !hit && yVelocity <= 0.0f && !coyoteJumpActivated)
        {
            coyoteJumpActivated = true;
            CoyoteJump();
        }

        previousGroundCast = hit;
    }

    private void CeilingDetection()
    {
        RaycastHit2D hit = Physics2D.BoxCast(rigidBody.position, new Vector2(2.0f * collider.bounds.extents.x, 0.1f), 0.0f, Vector2.up, castLength + collider.bounds.extents.y, solidsLayer);

        if (hit)
        {
            yVelocity = 0.0f;
        }
    }

    private void Jump()
    {
        yVelocity = jumpPower;
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

    private IEnumerator CoyoteJump()
    {
        float coyoteJumpTimer = 0.0f;
        float coyoteJumpDuration = 0.1f;

        while (coyoteJumpTimer < coyoteJumpDuration)
        {
            coyoteJumpTimer += Time.deltaTime;
            yield return null;
        }

        coyoteJumpActivated = false;
    }

    #endregion
}