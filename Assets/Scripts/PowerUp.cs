using UnityEngine;

/// <summary>
/// A dynamic power-up that the Cloud can collect.
/// Features animations (spawn, float, blink, despawn) and moving variants.
/// </summary>
public class PowerUp : MonoBehaviour
{
    public enum PowerUpType
    {
        Water, // Refill rain
        Speed  // Temporary acceleration
    }

    private enum PowerUpState
    {
        Spawning,
        Idle,
        Flashing,
        Despawning
    }

    [Header("Core Settings")]
    public PowerUpType type = PowerUpType.Water;
    public float amount = 30f; // Amount to enhance

    [Header("Lifetime & Animation Timings")]
    public float lifetime = 8f;            // Total time before disappearing
    public float spawnDuration = 0.5f;     // Time for elastic pop-in
    public float flashDuration = 2.0f;     // Starts flashing when life remaining <= this
    public float despawnDuration = 0.5f;   // Time to shrink before destruction

    [Header("Visuals, Movement & Variants")]
    [Tooltip("Assign the child Sprite GameObject so it can scale/flash independently of the Collider.")]
    public Transform spriteTransform;
    public float floatAmplitude = 0.15f;    // Up/down bobbing distance
    public float floatSpeed = 3f;          // Up/down bobbing speed
    
    [Tooltip("Give it a direction and speed to create moving power-up variants!")]
    public Vector3 moveDirection = Vector3.zero; 
    public float moveVelocity = 0f;

    [Header("Effects")]
    public AudioClip spawnSound;
    public GameObject collectEffect; // Particle/Sound effect on collection

    private PowerUpState state = PowerUpState.Spawning;
    private float stateTimer = 0f;
    private float totalAliveTime = 0f;
    
    private Vector3 initialScale;
    private Vector3 basePosition;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        // Try to automatically find a child visual if not assigned
        if (spriteTransform == null && transform.childCount > 0) 
        {
            spriteTransform = transform.GetChild(0);
        }

        if (spriteTransform != null)
        {
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
            initialScale = spriteTransform.localScale;
            spriteTransform.localScale = Vector3.zero; // Start shrunk
        }
        else 
        {
            // Fallback to self if no child sprite exists
            spriteRenderer = GetComponent<SpriteRenderer>();
            initialScale = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        basePosition = transform.position;

        // Ensure Physics overlap works robustly
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        // Play spawn sound
        if (AudioManager.Instance != null && spawnSound != null)
        {
            AudioManager.Instance.PlaySFXRandomPitch(spawnSound, 0.9f, 1.15f);
        }
    }

    private void Update()
    {
        totalAliveTime += Time.deltaTime;
        stateTimer += Time.deltaTime;

        HandleMovement();
        HandleStates();
    }

    private void HandleMovement()
    {
        // Floating (bobbing) effect calculated relative to the traveling basePosition
        float floatOffset = Mathf.Sin(totalAliveTime * floatSpeed) * floatAmplitude;
        
        // Continuous horizontal/diagonal drift for moving variants
        if (moveVelocity > 0f && moveDirection != Vector3.zero)
        {
             basePosition += moveDirection.normalized * moveVelocity * Time.deltaTime;
        }
        
        transform.position = basePosition + new Vector3(0f, floatOffset, 0f);
    }

    private void HandleStates()
    {
        float timeRemaining = lifetime - totalAliveTime;

        switch (state)
        {
            case PowerUpState.Spawning:
                // Elastic Easing calculation for pop-in scale
                float t = Mathf.Clamp01(stateTimer / spawnDuration);
                float elasticT = ElasticOut(t);
                ScaleTarget(initialScale * elasticT);

                if (t >= 1f)
                {
                    ChangeState(PowerUpState.Idle);
                }
                break;

            case PowerUpState.Idle:
                // Transition to flashing if time is running out
                if (timeRemaining <= (flashDuration + despawnDuration) && timeRemaining > despawnDuration)
                {
                    ChangeState(PowerUpState.Flashing);
                }
                else if (timeRemaining <= despawnDuration) // Failsafe
                {
                    ChangeState(PowerUpState.Despawning);
                }
                break;

            case PowerUpState.Flashing:
                // Fast blink effect by toggling Alpha (or visibility)
                if (spriteRenderer != null)
                {
                    // PingPong creates a pulsing wave. Speed it up by multiplying time.
                    float alpha = Mathf.PingPong(totalAliveTime * 20f, 1f) > 0.5f ? 1f : 0.2f;
                    Color c = spriteRenderer.color;
                    c.a = alpha;
                    spriteRenderer.color = c;
                }
                else if (spriteTransform != null)
                {
                    spriteTransform.gameObject.SetActive(Mathf.PingPong(totalAliveTime * 20f, 1f) > 0.5f);
                }

                // If flashing ends, transition to shrink-and-die
                if (timeRemaining <= despawnDuration)
                {
                    // Reset visuals to full visibility before shrinking
                    if (spriteRenderer != null)
                    {
                        Color c = spriteRenderer.color;
                        c.a = 1f;
                        spriteRenderer.color = c;
                    }
                    else if (spriteTransform != null)
                    {
                        spriteTransform.gameObject.SetActive(true);
                    }
                    
                    ChangeState(PowerUpState.Despawning);
                }
                break;

            case PowerUpState.Despawning:
                // Smooth scale down to 0 vector
                float despawnT = Mathf.Clamp01(stateTimer / despawnDuration);
                ScaleTarget(Vector3.Lerp(initialScale, Vector3.zero, despawnT));

                if (despawnT >= 1f)
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    private void ChangeState(PowerUpState newState)
    {
        state = newState;
        stateTimer = 0f;
    }

    private void ScaleTarget(Vector3 newScale)
    {
        if (spriteTransform != null)
        {
            spriteTransform.localScale = newScale;
        }
        else
        {
            transform.localScale = newScale;
        }
    }

    // Mathematical formula for a nice bouncy pop-in effect
    private float ElasticOut(float t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;
        float p = 0.3f;
        return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4) * (2 * Mathf.PI) / p) + 1;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Prevent pickup if we are already practically invisible and dying
        if (state == PowerUpState.Despawning && stateTimer > despawnDuration * 0.5f) return;

        // Search upward in case the cloud's collider is in a child object
        CloudController cloud = collision.GetComponentInParent<CloudController>();
        if (cloud != null)
        {
            cloud.CollectPowerUp(type.ToString(), amount);

            if (collectEffect != null)
            {
                // Play particle explosion/VFX
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            Destroy(gameObject); // Self destruct upon collection
        }
    }
}
