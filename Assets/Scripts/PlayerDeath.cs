using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerDeath : MonoBehaviour
{
    [Header("Respawn")]
    [Tooltip("Fallback respawn when the kill source has no trap-specific respawn point.")]
    public Transform respawnPoint;
    public float respawnDelay = 3f;

    [Header("UI")]
    public GameObject deathText;
    public Image fadeImage;

    [Header("Fade")]
    public float fadeDuration = 0.8f;

    [Header("Controller")]
    public MonoBehaviour firstPersonController;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip deathSound;

    private CharacterController characterController;
    private bool isDead = false;
    private Transform pendingRespawnPoint;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (deathText != null)
            deathText.SetActive(false);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0;
            fadeImage.color = c;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("KillZone"))
        {
            pendingRespawnPoint = ResolveRespawnPoint(other);
            StartCoroutine(DeathRoutine());
        }
    }

    private Transform ResolveRespawnPoint(Collider killZone)
    {
        KillTrapController trap = killZone.GetComponentInParent<KillTrapController>();
        if (trap != null && trap.RespawnPoint != null)
            return trap.RespawnPoint;

        return respawnPoint;
    }

    IEnumerator DeathRoutine()
    {
        isDead = true;

        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        if (firstPersonController != null)
        {
            firstPersonController.enabled = false;
        }

        if (deathText != null)
        {
            deathText.SetActive(true);
        }

        yield return StartCoroutine(FadeToBlack());

        yield return new WaitForSeconds(respawnDelay);

        Transform spawn = pendingRespawnPoint != null ? pendingRespawnPoint : respawnPoint;
        if (spawn != null)
        {
            characterController.enabled = false;

            transform.SetPositionAndRotation(
                spawn.position,
                spawn.rotation
            );

            characterController.enabled = true;
        }
        else
        {
            Debug.LogWarning("PlayerDeath: No respawn point assigned on the trap or PlayerDeath.", this);
        }

        if (deathText != null)
        {
            deathText.SetActive(false);
        }

        yield return StartCoroutine(FadeFromBlack());

        if (firstPersonController != null)
        {
            firstPersonController.enabled = true;
        }

        pendingRespawnPoint = null;
        isDead = false;
    }

    IEnumerator FadeToBlack()
    {
        if (fadeImage == null)
            yield break;

        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            Color c = fadeImage.color;
            c.a = Mathf.Lerp(0, 1, timer / fadeDuration);

            fadeImage.color = c;

            yield return null;
        }
    }

    IEnumerator FadeFromBlack()
    {
        if (fadeImage == null)
            yield break;

        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            Color c = fadeImage.color;
            c.a = Mathf.Lerp(1, 0, timer / fadeDuration);

            fadeImage.color = c;

            yield return null;
        }
    }
}