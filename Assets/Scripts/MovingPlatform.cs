using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private Rigidbody2D rigidbody;
    private float xValue = 0.0f;
    private float timer = 0.0f;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        xValue = Mathf.Sin(timer) * 5.5f + 5.5f;
        rigidbody.MovePosition(new Vector2(xValue, 0.0f));

        timer += Time.fixedDeltaTime;
    }
}
