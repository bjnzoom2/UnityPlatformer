using System.Collections;
using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    private Transform spriteTransform;
    private PlayerMovement movement;

    private float changeTime = 1.0f;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        spriteTransform = transform.GetChild(0);
    }

    private void OnEnable()
    {
        movement.OnJumpVisual += JumpStretch;
    }

    private void OnDisable()
    {
        movement.OnJumpVisual -= JumpStretch;
    }

    private void Update()
    {
        spriteTransform.localScale = Vector3.MoveTowards(spriteTransform.localScale, new Vector3(1.0f, 1.0f, 0.0f), 1.0f / changeTime * Time.deltaTime);
    }

    private void JumpStretch()
    {
        spriteTransform.localScale = new Vector3(0.8f, 1.2f, 0.0f);
    }
}
