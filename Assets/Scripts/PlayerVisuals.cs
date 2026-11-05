using System.Collections;
using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    private Transform pivotTransform;
    private PlayerMovement movement;

    private float changeTime = 1.5f;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        pivotTransform = transform.GetChild(0);
    }

    private void OnEnable()
    {
        movement.OnJumpVisual += JumpStretch;
        movement.OnLandVisual += LandSquash;
    }

    private void OnDisable()
    {
        movement.OnJumpVisual -= JumpStretch;
        movement.OnLandVisual -= LandSquash;
    }

    private void Update()
    {
        pivotTransform.localScale = Vector3.MoveTowards(pivotTransform.localScale, new Vector3(1.0f, 1.0f, 0.0f), 1.0f / changeTime * Time.deltaTime);
    }

    private void JumpStretch()
    {
        pivotTransform.localScale = new Vector3(0.8f, 1.2f, 0.0f);
    }

    private void LandSquash()
    {
        pivotTransform.localScale = new Vector3(1.2f, 0.8f, 0.0f);
    }
}
