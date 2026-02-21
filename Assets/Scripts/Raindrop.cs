using UnityEngine;

/// <summary>
/// Controls the behavior of individual falling raindrops.
/// </summary>
public class Raindrop : MonoBehaviour
{
    [Header("Movement Settings")]
    public float fallSpeed = 10f;

    [Header("Lifetime Settings")]
    public float maxLifetime = 5f; // Automatically destroy after this many seconds as a failsafe

    [Header("Visual Effects")]
    public GameObject splashEffectPrefab; // Water splash particles
    public GameObject puddlePrefab;       // Puddle left on the ground

    private float lifetimeTimer = 0f;
    private float targetGroundY = float.MinValue; // Used to check if we hit the "ground" based on 2.5D shadow

    void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // To ensure trigger events fire (e.g. hitting NPCs), one of the objects MUST have a Rigidbody2D.
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.isKinematic = true;
    }

    void Update()
    {
        HandleFall();
        HandleLifetime();
    }

    /// <summary>
    /// Handles the falling movement of the raindrop.
    /// </summary>
    private void HandleFall()
    {
        // Move the raindrop down
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        // Check if we reached the simulated ground level in our 2.5D space
        if (targetGroundY != float.MinValue && transform.position.y <= targetGroundY)
        {
            OnHitGround();
        }
    }

    /// <summary>
    /// Handles the maximum lifetime failsafe for performance and safety.
    /// </summary>
    private void HandleLifetime()
    {
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= maxLifetime)
        {
            // Destroy silently if it just times out
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets the target Y position where this raindrop should "hit the ground".
    /// This is useful for 2.5D where the ground isn't a simple physics collider below it.
    /// </summary>
    public void SetTargetGroundY(float groundY)
    {
        targetGroundY = groundY;
    }

    /// <summary>
    /// Unity Physics 2D trigger event.
    /// Called when the raindrop overlaps with another trigger or kinematic/dynamic collider.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // If we hit an object that belongs to the Ground layer
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.CompareTag("Ground"))
        {
            OnHitGround();
            return;
        }

        // If we hit an NPC
        if (other.gameObject.layer == LayerMask.NameToLayer("NPC") || other.CompareTag("NPC"))
        {
            // Spawn splash effect on the NPC but NO puddle
            if (splashEffectPrefab != null)
            {
                Instantiate(splashEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Note: NPCBase also has OnTriggerEnter2D to catch this and call GetWet()
            DestroyRaindrop();
            return;
        }

        // Destroy upon hitting anything else to prevent lingering colliders
        DestroyRaindrop();
    }

    /// <summary>
    /// Called when the raindrop reaches its calculated ground position OR physically touches the Ground layer.
    /// </summary>
    private void OnHitGround()
    {
        Vector3 groundPos = new Vector3(transform.position.x, targetGroundY != float.MinValue ? targetGroundY : transform.position.y, 0f);

        // Spawning the splash effect
        if (splashEffectPrefab != null)
        {
            Instantiate(splashEffectPrefab, transform.position, Quaternion.identity);
        }

        // Check if we hit an existing puddle first
        bool hitExistingPuddle = false;
        Collider2D[] hitColliders = Physics2D.OverlapPointAll(groundPos);
        foreach (var col in hitColliders)
        {
            Puddle puddle = col.GetComponent<Puddle>();
            if (puddle != null)
            {
                puddle.ModifySize(1f); // Enlarge existing puddle
                hitExistingPuddle = true;
                break;
            }
        }

        // Spawning the new puddle ONLY if we didn't land in one
        if (!hitExistingPuddle && puddlePrefab != null)
        {
            Instantiate(puddlePrefab, groundPos, Quaternion.identity);
        }

        DestroyRaindrop();
    }

    /// <summary>
    /// Centralized method for destroying the raindrop.
    /// </summary>
    private void DestroyRaindrop()
    {
        // Add any pooling logic here if you switch to Object Pooling in the future
        Destroy(gameObject);
    }
}
