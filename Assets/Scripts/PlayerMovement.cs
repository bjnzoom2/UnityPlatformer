using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;

public class PlayerMovement : MonoBehaviour
{
    #region Variables
    [SerializeField] private LayerMask solidsLayer;
    private Rigidbody2D rigidBody;
    private BoxCollider2D collider;
    private GameInputs inputs;

    private float xVelocity = 0.0f;
    private float yVelocity = 0.0f;

    private float horizontalInput = 0.0f;
    private float verticalInput = 0.0f;
    private float targetPlayerSpeed = 8.0f;

    private float accelTime = 0.1f;
    private float deccelTime = 0.05f;
    private float airResistanceValue = 0.5f;

    private float gravity = 0.6f;
    private float fallGravity = 0.8f;
    private float apexGravity = 0.4f;
    private float maxFallSpeed = 35.0f;

    private bool isGrounded = false;
    private bool jumpFlag = false;
    private float jumpPower = 16.0f;
    private float castLength = 0.003f;

    private bool jumpCutOff = false;

    private bool jumpBufferActivated = false;

    private bool coyoteJumpActivated = false;
    private bool previousGroundCast = false;

    private bool isWallSliding = false;
    private float xFacingDirection = 1.0f;
    private float wallSlideSpeed = 3.0f;
    private Vector2 wallSlideNormal = new Vector2();

    private bool dashFlag = false;
    private bool canDash = true;
    private bool dashActivated = false;
    private float dashSpeed = 16.0f;
    private bool dashCooldownActivated = false;

    #endregion

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
        verticalInput = inputs.Player.VeritcalInput.ReadValue<float>();

        if (!jumpFlag)
        {
            jumpFlag = inputs.Player.Jump.WasPressedThisFrame();
        }

        if (!jumpCutOff && rigidBody.linearVelocityY > 0.0f)
        {
            jumpCutOff = inputs.Player.Jump.WasReleasedThisFrame();
        }

        if (!dashFlag && !dashActivated && !dashCooldownActivated)
        {
            dashFlag = inputs.Player.Dash.WasPressedThisFrame();
        }

        Debug.DrawRay(transform.position, Vector2.down * (castLength + collider.bounds.extents.y), Color.red);
        Debug.DrawRay(transform.position, new Vector2(xFacingDirection, 0.0f) * (castLength + collider.bounds.extents.y), Color.red);
    }

    private void FixedUpdate()
    {
        GroundDetection();
        CeilingDetection();

        float targetXVelocity = horizontalInput * targetPlayerSpeed;
        if (!dashFlag && !dashActivated)
        {
            if (targetXVelocity != 0.0f)
            {
                if (isGrounded)
                {
                    xVelocity = Mathf.MoveTowards(xVelocity, targetXVelocity, targetPlayerSpeed / accelTime * Time.fixedDeltaTime);
                }
                else
                {
                    xVelocity = Mathf.MoveTowards(xVelocity, targetXVelocity, targetPlayerSpeed / (accelTime / airResistanceValue) * Time.fixedDeltaTime);
                }
            }
            else
            {
                if (isGrounded)
                {
                    xVelocity = Mathf.MoveTowards(xVelocity, targetXVelocity, targetPlayerSpeed / deccelTime * Time.fixedDeltaTime);
                }
                else
                {
                    xVelocity = Mathf.MoveTowards(xVelocity, targetXVelocity, targetPlayerSpeed / (deccelTime / airResistanceValue) * Time.fixedDeltaTime);
                }
            }

            if (horizontalInput != 0.0f)
            {
                xFacingDirection = horizontalInput;
            }
        } else if (dashFlag && !dashActivated && canDash)
        {
            dashFlag = false;
            dashActivated = true;
            StartCoroutine(Dash());
        }

        WallSlideDetection(out Vector2 _normal);
        wallSlideNormal = _normal;
        
        if (isGrounded || isWallSliding && !dashCooldownActivated && !dashActivated)
        {
            canDash = true;
        }

        if (jumpFlag && !dashActivated)
        {
            jumpFlag = false;
            if (isWallSliding)
            {
                WallJump(wallSlideNormal);
            } else if (coyoteJumpActivated)
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

        if (!dashActivated)
        {
            HandleGravity();
        }

        if (isWallSliding)
        {
            yVelocity = Mathf.Clamp(yVelocity, -wallSlideSpeed, 0.0f);
        }

        yVelocity = Mathf.Clamp(yVelocity, -maxFallSpeed, Mathf.Infinity);

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
            StartCoroutine(CoyoteJump());
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

    private void HandleGravity()
    {
        if (yVelocity <= 0.15f && yVelocity >= -0.15f)
        {
            yVelocity -= apexGravity;
        } else if (yVelocity < 0.15f)
        {
            yVelocity -= fallGravity;
        }
        else
        {
            yVelocity -= gravity;
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

            if (isWallSliding)
            {
                WallJump(wallSlideNormal);
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

    private void WallSlideDetection(out Vector2 wallNormal)
    {
        RaycastHit2D hit = Physics2D.BoxCast(rigidBody.position, new Vector2(0.1f, 2.0f * collider.bounds.extents.y), 0.0f, new Vector2(xFacingDirection, 0.0f), castLength + collider.bounds.extents.x, solidsLayer);
        wallNormal = hit.normal;

        if (hit && yVelocity < 0.0f && !dashActivated)
        {
            isWallSliding = true;
        } else
        {
            isWallSliding = false;
        }
    }

    private void WallJump(Vector2 wallNormal)
    {
        xVelocity = wallNormal.x * jumpPower;
        yVelocity = jumpPower;
        xFacingDirection = -xFacingDirection;
        isWallSliding = false;
    }

    private IEnumerator Dash()
    {
        canDash = false;

        float dashTimer = 0.0f;
        float dashDuration = 0.2f;
        Vector2 dashVector = new Vector2();

        if (isGrounded)
        {
            dashVector = new Vector2(xFacingDirection * dashSpeed, 0.0f);
        } else
        {
            dashVector = new Vector2(horizontalInput, verticalInput);
            if (horizontalInput == 0.0f && verticalInput == 0.0f)
            {
                dashVector.x = xFacingDirection;
            }
            dashVector = dashVector.normalized * dashSpeed;
        }

        while (dashTimer < dashDuration)
        {
            xVelocity = dashVector.x;
            yVelocity = dashVector.y;

            dashTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        dashActivated = false;
        StartCoroutine(DashCooldown());
    }

    private IEnumerator DashCooldown()
    {
        dashCooldownActivated = true;
        float timer = 0.0f;
        float cooldown = 0.5f;

        while (timer < cooldown)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        dashCooldownActivated = false;
    }
}