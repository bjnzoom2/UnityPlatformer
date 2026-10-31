using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rigidBody;
    private GameInputs inputs;

    private float targetPlayerSpeed = 8.0f;
    private float jumpPower = 10.0f;
    private float xVelocity = 0.0f;
    private float accelTime = 0.1f;

    private float horizontalInput;
    private bool hasJumped = false;

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
        if (!hasJumped)
        {
            hasJumped = inputs.Player.Jump.WasPressedThisFrame();
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
            rigidBody.linearVelocityY = jumpPower;
            hasJumped = false;
        }
    }
}
