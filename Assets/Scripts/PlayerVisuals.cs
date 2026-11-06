using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerVisuals : MonoBehaviour
{
    private Transform pivotTransform;
    private SpriteRenderer renderer;
    [SerializeField] private ParticleSystem deathParticlePrefab;
    [SerializeField] private Volume postProcessing;
    private PlayerMovement movement;

    private float deformVelo = 0.0f;
    private float deformValue = 0.0f;
    private float targetDeform = 0.0f;
    private float holdTimer = 0.0f;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        pivotTransform = transform.Find("Pivot");
        renderer = pivotTransform.Find("Square").GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        movement.OnJumpVisual += JumpStretch;
        movement.OnLandVisual += LandStretch;
        movement.OnDashVisual += DashStretch;
        movement.OnDashHitVisual += () => {
            StartCoroutine(DashHit());
        };
        movement.OnDeathVisual += Death;
    }

    private void OnDisable()
    {
        movement.OnJumpVisual -= JumpStretch;
        movement.OnLandVisual -= LandStretch;
        movement.OnDashVisual -= DashStretch;
        movement.OnDashHitVisual -= () => {
            StartCoroutine(DashHit());
        };
        movement.OnDeathVisual -= Death;
    }

    private void Update()
    {
        if (holdTimer > 0.0f)
        {
            holdTimer -= Time.deltaTime;
        } else
        {
            holdTimer = 0.0f;
            targetDeform = 0.0f;
        }

        deformValue = Mathf.SmoothDamp(deformValue, targetDeform, ref deformVelo, 0.08f);

        pivotTransform.localScale = new Vector3(1.0f - deformValue, 1.0f + deformValue, 0.0f);
    }

    private void JumpStretch()
    {
        targetDeform = 0.2f;
        holdTimer = 0.08f;
    }

    private void LandStretch()
    {
        targetDeform = -0.2f;
        holdTimer = 0.08f;
    }

    private void DashStretch(Vector2 dashVector)
    {
        if (dashVector.y == 0.0f)
        {
            targetDeform = -0.3f;
        }
        else if (dashVector.x == 0.0f)
        {
            targetDeform = 0.3f;
        } else
        {
            targetDeform = 0.0f;
        }
        holdTimer = 0.15f;
    }

    private IEnumerator DashHit()
    {
        yield return null;
        Time.timeScale = 0.0f;
        yield return new WaitForSecondsRealtime(0.04f);
        Time.timeScale = 1.0f;
    }

    private void Death()
    {
        Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
        renderer.enabled = false;
        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        float timer = 0.0f;
        float duration = 1.5f;
        postProcessing.profile.TryGet(out ColorAdjustments adjustments);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float value = Mathf.InverseLerp(0.0f, duration, timer);
            Time.timeScale = 1.0f - value;
            adjustments.saturation.value = (1.0f - value) * 100.0f - 100.0f;

            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        transform.position = new Vector3(movement.respawnPoint.transform.position.x, movement.respawnPoint.transform.position.y, 0.0f);

        renderer.enabled = true;
        movement.playerState = PlayerMovement.States.Airborne;

        Time.timeScale = 1.0f;
        adjustments.saturation.value = 0.0f;
    }
}
