using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerDeath : MonoBehaviour
{
    [Header("Respawn")]
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
            StartCoroutine(DeathRoutine());
        }
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

        characterController.enabled = false;

        transform.SetPositionAndRotation(
            respawnPoint.position,
            respawnPoint.rotation
        );

        characterController.enabled = true;

        if (deathText != null)
        {
            deathText.SetActive(false);
        }

        yield return StartCoroutine(FadeFromBlack());

        if (firstPersonController != null)
        {
            firstPersonController.enabled = true;
        }

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