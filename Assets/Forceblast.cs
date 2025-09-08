using UnityEngine;
using System.Collections;

public class PlayerExplosion : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionForce = 500f;
    public float explosionRadius = 5f;
    public float upwardsModifier = 1f;
    public ForceMode forceMode = ForceMode.Impulse;

    [Header("Cooldown Settings")]
    public float cooldownTime = 2f;
    private bool canExplode = true;

    [Header("Visual & Audio (Optional)")]
    public GameObject explosionPrefab;
    public AudioClip explosionSound;
    public float animationDuration = 1f;

    private AudioSource audioSource;

    void Start()
    {
        if (explosionSound)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = explosionSound;
        }
    }

    void Update()
    {
        if (canExplode && Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(DoExplosion());
        }
    }

    IEnumerator DoExplosion()
    {
        canExplode = false;
        Vector3 pos = transform.position;

        // Optional visual effect
        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, pos, Quaternion.identity);
            Destroy(fx, animationDuration);
        }

        // Optional sound effect
        if (audioSource)
            audioSource.Play();

        // Apply physics force
        Collider[] colliders = Physics.OverlapSphere(pos, explosionRadius);
        foreach (var hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, pos, explosionRadius, upwardsModifier, forceMode);
            }
        }

        yield return new WaitForSeconds(cooldownTime);
        canExplode = true;
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
