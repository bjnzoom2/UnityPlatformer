using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region Old Logic
    //[SerializeField] private LayerMask groundLayer;
    //private Rigidbody2D rigidBody;
    //private BoxCollider2D collider;
    //private GameInputs inputs;

    //private float targetPlayerSpeed = 8.0f;
    //private float jumpPower = 16.0f;
    //private float xVelocity = 0.0f;
    //private float accelTime = 0.1f;

    //private float horizontalInput;
    //private bool hasJumped = false;
    //private bool isGrounded = false;
    //private bool jumpCutOff = false;
    //private bool jumpBufferActivated = false;

    //private void Awake()
    //{
    //    rigidBody = GetComponent<Rigidbody2D>();
    //    collider = GetComponent<BoxCollider2D>();
    //    inputs = new GameInputs();
    //}

    //private void OnEnable()
    //{
    //    inputs.Enable();
    //}

    //private void Start()
    //{

    //}

    //private void Update()
    //{
    //    horizontalInput = inputs.Player.HorizontalMovement.ReadValue<float>();

    //    float raylength = 0.01f;
    //    RaycastHit2D hit = Physics2D.BoxCast(collider.bounds.center, new Vector2(2.0f * collider.bounds.extents.x, 0.1f), 0.0f, Vector2.down, raylength + collider.bounds.extents.y, groundLayer);
    //    Debug.DrawRay(collider.bounds.center, Vector2.down * (raylength + collider.bounds.extents.y), Color.red);

    //    if (hit.collider != null && rigidBody.linearVelocityY <= 0.05f)
    //    {
    //        isGrounded = true;
    //    } else
    //    {
    //        isGrounded = false;
    //    }

    //    if (!hasJumped)
    //    {
    //        hasJumped = inputs.Player.Jump.WasPressedThisFrame();
    //    }

    //    if (!jumpCutOff && !isGrounded)
    //    {
    //        jumpCutOff = (inputs.Player.Jump.WasReleasedThisFrame() && rigidBody.linearVelocityY > 0.0f);
    //    }
    //}

    //private void FixedUpdate()
    //{
    //    xVelocity = rigidBody.linearVelocityX;
    //    float targetXVelocity = horizontalInput * targetPlayerSpeed;
    //    xVelocity = Mathf.MoveTowards(xVelocity, targetXVelocity, targetPlayerSpeed / accelTime * Time.fixedDeltaTime);
    //    rigidBody.linearVelocityX = xVelocity;

    //    if (hasJumped)
    //    {
    //        hasJumped = false;
    //        if (isGrounded && !jumpBufferActivated)
    //        {
    //            Jump();
    //        } else if (!jumpBufferActivated)
    //        {
    //            jumpBufferActivated = true;
    //            StartCoroutine(JumpBuffer());
    //        }
    //    }

    //    if (jumpCutOff)
    //    {
    //        rigidBody.linearVelocityY *= 0.6f;
    //        jumpCutOff = false;
    //    }
    //}

    //private void Jump()
    //{
    //    rigidBody.AddForceY(jumpPower, ForceMode2D.Impulse);
    //}

    //private IEnumerator JumpBuffer()
    //{
    //    float jumpBufferTimer = 0.0f;
    //    float jumpBufferDuration = 0.1f;

    //    while (jumpBufferTimer < jumpBufferDuration)
    //    {
    //        if (isGrounded)
    //        {
    //            Jump();
    //            jumpBufferActivated = false;
    //            yield break;
    //        }

    //        jumpBufferTimer += Time.deltaTime;
    //        yield return null;
    //    }

    //    jumpBufferActivated = false;
    //}
    #endregion

    private Rigidbody2D rigidBody;
    private GameInputs inputs;
    [SerializeField] private LayerMask groundLayer;

    private float targetPlayerSpeed = 8.0f;
    private float horizontalInput;

    private Vector2 displacementVector;
    private float xVelocity = 0.0f;
    private float yVelocity = 0.0f;
    private float xAccelTime = 0.1f;
    private float xDeccelTime = 0.05f;
    private float xSkidTime = 0.033f;

    private float gravity = 1.0f;
    private bool isGrounded = false;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
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
    }

    private void FixedUpdate()
    {
        float targetXVelocity = horizontalInput * targetPlayerSpeed;
        if (targetXVelocity != 0.0f)
        {
            if (xVelocity * horizontalInput < 0.0f)
            {
                xVelocity = Mathf.MoveTowards(xVelocity, 0.0f, targetPlayerSpeed / xSkidTime);
            } else
            {
                xVelocity = Mathf.MoveTowards(xVelocity, targetXVelocity, targetPlayerSpeed / xAccelTime);
            }
        } else
        {
            xVelocity = Mathf.MoveTowards(xVelocity, targetXVelocity, targetPlayerSpeed / xDeccelTime);
        }

        yVelocity -= gravity;

        displacementVector = new Vector2(xVelocity, yVelocity) * Time.fixedDeltaTime;

        HandleXCollision();
        HandleYCollision();

        rigidBody.MovePosition(rigidBody.position + displacementVector);
    }

    private void HandleXCollision()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.useTriggers = false;
        filter.layerMask = groundLayer;

        List<RaycastHit2D> hits = new List<RaycastHit2D>();
        int hitCheck = rigidBody.Cast(new Vector2(Mathf.Sign(displacementVector.x), 0.0f), filter, hits, Mathf.Abs(displacementVector.x));

        if (hitCheck > 0)
        {
            float distance = Mathf.Abs(displacementVector.x) + 1.0f;

            for (int i = 0; i < hitCheck; i++)
            {
                if (hits[i].distance < distance)
                {
                    distance = hits[i].distance;
                }
            }

            displacementVector.x = Mathf.Sign(displacementVector.x) * (distance - 0.003f);
        }
    }

    private void HandleYCollision()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.useTriggers = false;
        filter.layerMask = groundLayer;

        List<RaycastHit2D> hits = new List<RaycastHit2D>();
        int hitCheck = rigidBody.Cast(new Vector2(0.0f, Mathf.Sign(displacementVector.y)), filter, hits, Mathf.Abs(displacementVector.y));

        if (hitCheck > 0)
        {
            float distance = Mathf.Abs(displacementVector.y) + 1.0f;

            for (int i = 0; i < hitCheck; i++)
            {
                if (hits[i].distance < distance)
                {
                    distance = hits[i].distance;
                }
            }

            if (Mathf.Sign(displacementVector.y) == -1.0f)
            {
                isGrounded = true;
            }

            yVelocity = 0.0f;

            displacementVector.y = Mathf.Sign(displacementVector.y) * (distance - 0.003f);
        } else
        {
            isGrounded = false;
        }
    }
}