using UnityEngine;
using System.Collections;
using System;

public class PlayerMovement : MonoBehaviour
{
    public enum States
    {
        Grounded = 0,
        Airborne = 1,
        WallSliding = 2,
        Dashing = 3
    }

    #region Variables

    public States playerState = new();

    [SerializeField] private LayerMask solidsLayer;
    [SerializeField] private LayerMask hazardsLayer;
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

    private bool jumpFlag = false;
    private float jumpPower = 16.0f;
    private float castLength = 0.003f;

    private bool jumpCutOff = false;

    private bool jumpBufferActivated = false;

    private bool coyoteJumpActivated = false;
    private bool previousGroundCast = false;

    private float xFacingDirection = 1.0f;
    private float wallSlideSpeed = 3.0f;
    private Vector2 wallSlideNormal = new Vector2();

    private bool dashFlag = false;
    private bool canDash = true;
    private float dashSpeed = 16.0f;
    private bool dashCooldownActivated = false;

    private bool leftCornerCorrectionHit = false;
    private bool rightCornerCorrectionHit = false;
    private float cornerCorrectionRayOffset = 0.7f;

    private bool leftLedgeHopHit = false;
    private bool rightLedgeHopHit = false;
    private float ledgeHopRayOffset = 1.15f;

    #endregion

    #region Events

    public event Action OnJumpVisual;
    public event Action OnLandVisual;

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

        if (!dashFlag && canDash)
        {
            dashFlag = inputs.Player.Dash.WasPressedThisFrame();
        }

        Debug.DrawRay(transform.position, Vector2.down * (castLength + collider.bounds.extents.y), Color.red);
        Debug.DrawRay(transform.position, new Vector2(xFacingDirection, 0.0f) * (castLength + collider.bounds.extents.y), Color.red);

        Debug.DrawRay(transform.position - new Vector3(collider.bounds.extents.x * cornerCorrectionRayOffset, 0.0f, 0.0f), Vector2.up * (castLength + collider.bounds.extents.y + 0.1f), Color.red);
        Debug.DrawRay(transform.position + new Vector3(collider.bounds.extents.x * cornerCorrectionRayOffset, 0.0f, 0.0f), Vector2.up * (castLength + collider.bounds.extents.y + 0.1f), Color.red);

        Debug.DrawRay(transform.position - new Vector3(collider.bounds.extents.x * ledgeHopRayOffset, 0.0f, 0.0f), Vector2.down * (castLength + collider.bounds.extents.y + 0.1f), Color.red);
        Debug.DrawRay(transform.position + new Vector3(collider.bounds.extents.x * ledgeHopRayOffset, 0.0f, 0.0f), Vector2.down * (castLength + collider.bounds.extents.y + 0.1f), Color.red);
    }

    private void FixedUpdate()
    {
        LedgeHopDetection();
        GroundDetection();
        CornerCorrectionDetection();
        CeilingDetection();

        float targetXVelocity = horizontalInput * targetPlayerSpeed;
        if (!dashFlag && playerState != States.Dashing)
        {
            if (targetXVelocity != 0.0f)
            {
                if (playerState == States.Grounded)
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
                if (playerState == States.Grounded)
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
        } else if (dashFlag && playerState != States.Dashing && canDash)
        {
            dashFlag = false;
            StartCoroutine(Dash());
        }

        WallSlideDetection(out Vector2 _normal);
        wallSlideNormal = _normal;
        
        if ((playerState == States.Grounded || playerState == States.WallSliding) && !dashCooldownActivated)
        {
            canDash = true;
        }

        if (jumpFlag && playerState != States.Dashing)
        {
            jumpFlag = false;
            if (playerState == States.WallSliding)
            {
                WallJump(wallSlideNormal);
            } else if (coyoteJumpActivated)
            {
                Jump();
                coyoteJumpActivated = false;
            } else if (!jumpBufferActivated)
            {
                if (playerState == States.Grounded)
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

        if (playerState != States.Dashing)
        {
            HandleGravity();
        }

        if (playerState == States.WallSliding)
        {
            yVelocity = Mathf.Clamp(yVelocity, -wallSlideSpeed, 0.0f);
        }

        yVelocity = Mathf.Clamp(yVelocity, -maxFallSpeed, Mathf.Infinity);

        rigidBody.linearVelocity = new Vector2(xVelocity, yVelocity);
    }

    private void GroundDetection()
    {
        RaycastHit2D hit = Physics2D.BoxCast(rigidBody.position, new Vector2(2.0f * collider.bounds.extents.x, 0.1f), 0.0f, Vector2.down, castLength + collider.bounds.extents.y, solidsLayer);

        if (hit && yVelocity <= 0.0f && playerState != States.Dashing)
        {
            playerState = States.Grounded;
            yVelocity = 0.0f;
        } else
        {
            if ((leftLedgeHopHit || rightLedgeHopHit) && !hit && playerState != States.Dashing)
            {
                playerState = States.Grounded;
            } else
            {
                playerState = States.Airborne;
            }
        }

        if (previousGroundCast && !hit)
        {
            if (yVelocity <= 0.0f && !coyoteJumpActivated)
            {
                coyoteJumpActivated = true;
                StartCoroutine(CoyoteJump());
            }
        } else if (!previousGroundCast && hit)
        {
            OnLandVisual?.Invoke();
        }

        previousGroundCast = hit;
    }

    private void CeilingDetection()
    {
        RaycastHit2D hit = Physics2D.BoxCast(rigidBody.position, new Vector2(2.0f * collider.bounds.extents.x, 0.1f), 0.0f, Vector2.up, castLength + collider.bounds.extents.y, solidsLayer);

        if (hit && playerState != States.Dashing)
        {
            if (leftCornerCorrectionHit || rightCornerCorrectionHit)
            {
                yVelocity = 0.0f;
            } else
            {
                if (hit.point.x > rigidBody.position.x)
                {
                    rigidBody.position -= new Vector2(collider.bounds.extents.x - (hit.point.x - rigidBody.position.x) + 0.01f, 0.0f);
                } else
                {
                    rigidBody.position += new Vector2(collider.bounds.extents.x - (rigidBody.position.x - hit.point.x) + 0.01f, 0.0f);
                }
            }
        }
    }

    private void CornerCorrectionDetection()
    {
        leftCornerCorrectionHit = Physics2D.Raycast(rigidBody.position - new Vector2(collider.bounds.extents.x * cornerCorrectionRayOffset, 0.0f), Vector2.up, castLength + collider.bounds.extents.y + 0.1f, solidsLayer);
        rightCornerCorrectionHit = Physics2D.Raycast(rigidBody.position + new Vector2(collider.bounds.extents.x * cornerCorrectionRayOffset, 0.0f), Vector2.up, castLength + collider.bounds.extents.y + 0.1f, solidsLayer);
    }

    private void LedgeHopDetection()
    {
        leftLedgeHopHit = Physics2D.Raycast(rigidBody.position - new Vector2(collider.bounds.extents.x * ledgeHopRayOffset, 0.0f), Vector2.down, castLength + collider.bounds.extents.y + 0.1f, solidsLayer);
        rightLedgeHopHit = Physics2D.Raycast(rigidBody.position + new Vector2(collider.bounds.extents.x * ledgeHopRayOffset, 0.0f), Vector2.down, castLength + collider.bounds.extents.y + 0.1f, solidsLayer);
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
        OnJumpVisual?.Invoke();
    }

    private IEnumerator JumpBuffer()
    {
        float jumpBufferTimer = 0.0f;
        float jumpBufferDuration = 0.1f;

        while (jumpBufferTimer < jumpBufferDuration)
        {
            if (playerState == States.Grounded)
            {
                Jump();
                jumpBufferActivated = false;
                yield break;
            }

            if (playerState == States.WallSliding)
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

        if (hit && yVelocity < 0.0f && playerState != States.Dashing)
        {
            playerState = States.WallSliding;
        }
    }

    private void WallJump(Vector2 wallNormal)
    {
        xVelocity = wallNormal.x * jumpPower;
        yVelocity = jumpPower;
        xFacingDirection = -xFacingDirection;
        playerState = States.Airborne;
    }

    private IEnumerator Dash()
    {
        canDash = false;

        float dashTimer = 0.0f;
        float dashDuration = 0.25f;
        Vector2 dashVector = new Vector2();

        if (playerState == States.Grounded)
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

        playerState = States.Dashing;

        while (dashTimer < dashDuration)
        {
            RaycastHit2D hit = Physics2D.BoxCast(rigidBody.position, collider.bounds.size, 0.0f, dashVector.normalized, dashVector.magnitude * Time.fixedDeltaTime + castLength, solidsLayer);

            float dashSlowDown = Mathf.Lerp(0.005f, dashDuration, dashTimer) * Mathf.Lerp(0.005f, dashDuration, dashTimer) * Mathf.Lerp(0.005f, dashDuration, dashTimer) * Mathf.Lerp(0.005f, dashDuration, dashTimer);
            dashVector *= 1.0f - dashSlowDown;

            xVelocity = dashVector.x;
            yVelocity = dashVector.y;

            if (hit)
            {
                Vector2 hitNormal = hit.normal;
                hitNormal = -1.0f * new Vector2(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y));

                if (hitNormal.x <= 0.1f && hitNormal.x >= -0.1f)
                {
                    hitNormal.x = 1.0f;
                }
                if (hitNormal.y <= 0.1f && hitNormal.y >= -0.1f)
                {
                    hitNormal.y = 1.0f;
                }

                dashVector *= hitNormal;
            }

            dashTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        yVelocity *= 0.7f;
        playerState = States.Airborne;
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer(hazardsLayer.ToString()))
        {
            Debug.Log("Death");
            Destroy(gameObject);
        }
    }
}