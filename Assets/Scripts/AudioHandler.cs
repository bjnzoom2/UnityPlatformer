using UnityEngine;

public class AudioHandler : MonoBehaviour
{
    [SerializeField] private AudioClip jumpSfx;
    [SerializeField] private AudioClip landSfx;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        PlayerMovement.OnJumpAudio += PlayJump;
        PlayerMovement.OnLandAudio += PlayLand;
    }

    private void OnDisable()
    {
        PlayerMovement.OnJumpAudio -= PlayJump;
        PlayerMovement.OnLandAudio -= PlayLand;
    }

    private void PlayJump()
    {
        audioSource.PlayOneShot(jumpSfx, 0.9f);
    }

    private void PlayLand()
    {
        audioSource.PlayOneShot(landSfx, 1.0f);
    }
}
