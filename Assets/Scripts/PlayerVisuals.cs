using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.DebugUI;

public class PlayerVisuals : MonoBehaviour
{
    private Transform pivotTransform;
    private SpriteRenderer renderer;
    [SerializeField] private ParticleSystem deathParticlePrefab;
    [SerializeField] private Volume postProcessing;
    private PlayerMovement movement;

    private float changeTime = 1.5f;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        pivotTransform = transform.Find("Pivot");
        renderer = pivotTransform.Find("Square").GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        movement.OnJumpVisual += JumpStretch;
        movement.OnLandVisual += LandSquash;
        movement.OnDeathVisual += Death;
    }

    private void OnDisable()
    {
        movement.OnJumpVisual -= JumpStretch;
        movement.OnLandVisual -= LandSquash;
        movement.OnDeathVisual -= Death;
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
