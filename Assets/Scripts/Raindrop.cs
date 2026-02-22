using UnityEngine;

public class Raindrop : MonoBehaviour
{
    [Header("Settings")]
    public float fallSpeed = 10f;
    public float maxLifetime = 5f; 

    [Header("Visuals")]
    public GameObject splashEffectPrefab; 
    public GameObject puddlePrefab;       

    [Header("Impact")]
    public float puddleEnlargeAmount = 1.0f;

    [Header("Audio")]
    public AudioClip dropHitSound;

    private float lifetimeTimer = 0f;
    private float targetGroundY = float.MinValue; 

    void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;
    }

    void Update()
    {
        HandleFall();
        HandleLifetime();
    }

    private void HandleFall()
    {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
        if (targetGroundY != float.MinValue && transform.position.y <= targetGroundY) OnHitGround();
    }

    private void HandleLifetime()
    {
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= maxLifetime) Destroy(gameObject);
    }

    public void SetTargetGroundY(float groundY)
    {
        targetGroundY = groundY;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<CloudController>() != null) return;
        if (other.GetComponent<Raindrop>() != null) return;
        if (other.GetComponent<PowerUp>() != null) return;

        Puddle puddle = other.GetComponent<Puddle>();
        if (puddle != null)
        {
            puddle.ModifySize(puddleEnlargeAmount); 
            if (splashEffectPrefab != null) Instantiate(splashEffectPrefab, transform.position, Quaternion.identity);
            if (AudioManager.Instance != null && dropHitSound != null) AudioManager.Instance.PlaySFXRandomPitch(dropHitSound, 0.8f, 1.2f);
            DestroyRaindrop();
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("NPC") || other.CompareTag("NPC"))
        {
            if (splashEffectPrefab != null) Instantiate(splashEffectPrefab, transform.position, Quaternion.identity);
            DestroyRaindrop();
            return;
        }
    }

    private void OnHitGround()
    {
        Vector3 groundPos = new Vector3(transform.position.x, targetGroundY != float.MinValue ? targetGroundY : transform.position.y, 0f);

        if (AudioManager.Instance != null && dropHitSound != null) AudioManager.Instance.PlaySFXRandomPitch(dropHitSound, 0.8f, 1.2f);
        if (splashEffectPrefab != null) Instantiate(splashEffectPrefab, transform.position, Quaternion.identity);

        bool hitExistingPuddle = false;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(groundPos, 0.5f);
        foreach (var col in hitColliders)
        {
            Puddle puddle = col.GetComponent<Puddle>();
            if (puddle != null)
            {
                puddle.ModifySize(puddleEnlargeAmount); 
                hitExistingPuddle = true;
                break;
            }
        }

        if (!hitExistingPuddle && puddlePrefab != null) Instantiate(puddlePrefab, groundPos, Quaternion.identity);
        DestroyRaindrop();
    }

    private void DestroyRaindrop()
    {
        Destroy(gameObject);
    }
}
