using UnityEngine;

/// <summary>
/// Base class for NPCs. Handles wandering, getting rained on, stepping in puddles, and leaving the map.
/// Can be inherited by specific NPC variants (e.g. FastNPC, WaterLovingNPC).
/// </summary>
public class NPCBase : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;

    [Header("Mood Impact")]
    public float rainMoodPenalty = -10f;
    public float puddleMoodBonus = 15f;
    
    [Header("Interactions & AI")]
    public float puddleShrinkAmount = 1.5f; // How much puddle size is consumed when stepped on
    public float puddleDetectionRadius = 4f; // Radius to look for puddles
    public LayerMask puddleLayer; // Optional: restrict detection to a specific layer

    [Header("Emotion Bubbles")]
    public GameObject bubbleFoundPuddle; // Triggered when finding puddle
    public GameObject bubbleHappy; // Triggered when satisfied
    public GameObject bubbleSad; // Triggered when rained on

    [Header("Emotion Audio")]
    public AudioClip soundFoundPuddle;
    public AudioClip soundHappy;
    public AudioClip soundSad;
    public AudioClip puddleSplashSound; // Plays when touching water

    [Header("Animation & Visuals")]
    [Tooltip("Uses custom 2-frame sprite swapping if assigned.")]
    public SpriteRenderer mainSpriteRenderer;
    public Sprite[] walkSprites; // child_idle, child_walk
    public Sprite[] jumpSprites; // child_crouch, child_jump
    public float walkAnimSpeed = 0.25f;
    public float runAnimSpeed = 0.12f;
    public float jumpAnimSpeed = 0.3f;
    
    private float animTimer = 0f;
    private int currentFrame = 0;

    public Animator anim;
    [Tooltip("If no animator is assigned, this sprite will simply bounce up and down when playing in water.")]
    public Transform spriteVisual;
    public float playTime = 1.0f; // How long they play in the water before leaving
    
    // Animation triggers
    public string animParamIsRunning = "IsRunning";
    public string animTriggerFoundPuddle = "FoundPuddle";
    public string animTriggerPlayWater = "PlayWater";
    public string animTriggerHappyLeave = "HappyLeave";
    public string animTriggerSadLeave = "SadLeave";

    protected enum NPCState { Wandering, PlayingInWater, Fleeing, Satisfied }
    protected NPCState currentState = NPCState.Wandering;

    protected Vector2 targetPosition;
    protected Puddle targetPuddle;

    protected virtual void Start()
    {
        // Register spawn with GameManager for scoring
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterNPCSpawn();
        }

        // Ensure NPC has a Kinematic Rigidbody2D so triggers (Puddles/Rain) can accurately fire
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.isKinematic = true;

        // Hide all bubbles initially
        ShowBubble(null);

        // Pick the first random spot to wander to
        PickNewWanderTarget();
    }

    protected virtual void ShowBubble(GameObject activeBubble)
    {
        if (bubbleFoundPuddle != null) bubbleFoundPuddle.SetActive(bubbleFoundPuddle == activeBubble);
        if (bubbleHappy != null) bubbleHappy.SetActive(bubbleHappy == activeBubble);
        if (bubbleSad != null) bubbleSad.SetActive(bubbleSad == activeBubble);

        if (AudioManager.Instance != null && activeBubble != null)
        {
            if (activeBubble == bubbleFoundPuddle && soundFoundPuddle != null) AudioManager.Instance.PlaySFXRandomPitch(soundFoundPuddle);
            else if (activeBubble == bubbleHappy && soundHappy != null) AudioManager.Instance.PlaySFXRandomPitch(soundHappy);
            else if (activeBubble == bubbleSad && soundSad != null) AudioManager.Instance.PlaySFXRandomPitch(soundSad);
        }
    }

    protected virtual void Update()
    {
        // Safety culling: if NPC wanders too far off map, destroy it
        if (GameManager.Instance != null && GameManager.Instance.mapBounds.size != Vector3.zero)
        {
            Bounds cullBounds = GameManager.Instance.mapBounds;
            cullBounds.Expand(10f); // 5 units of padding on all sides before wiping memory
            if (!cullBounds.Contains(transform.position))
            {
                Destroy(gameObject);
                return;
            }
        }

        switch (currentState)
        {
            case NPCState.Wandering:
                Wander();
                break;
            case NPCState.PlayingInWater:
                // Stand still while playing animation/routine
                break;
            case NPCState.Fleeing:
            case NPCState.Satisfied:
                RunOffMap();
                break;
        }

        HandleSpriteAnimation();
    }

    protected virtual void HandleSpriteAnimation()
    {
        if (mainSpriteRenderer == null) return;
        
        animTimer += Time.deltaTime;
        float currentSpeed = walkAnimSpeed;
        Sprite[] currentArray = walkSprites;

        switch (currentState)
        {
            case NPCState.Wandering:
                currentSpeed = walkAnimSpeed;
                currentArray = walkSprites;
                break;
            case NPCState.PlayingInWater:
                currentSpeed = jumpAnimSpeed;
                currentArray = jumpSprites;
                break;
            case NPCState.Fleeing:
            case NPCState.Satisfied:
                currentSpeed = runAnimSpeed; // Fast playback
                currentArray = walkSprites;
                break;
        }

        if (currentArray != null && currentArray.Length > 0)
        {
            if (animTimer >= currentSpeed)
            {
                animTimer = 0f;
                currentFrame = (currentFrame + 1) % currentArray.Length;
                mainSpriteRenderer.sprite = currentArray[currentFrame];
            }
        }

        base.transform.position = transform.position; // redundant safeguard
        
        // Flip sprite based on movement direction (assuming source material defaults to facing LEFT)
        if (currentState != NPCState.PlayingInWater)
        {
            if (targetPosition.x > transform.position.x + 0.05f) mainSpriteRenderer.flipX = true;  // moving right
            else if (targetPosition.x < transform.position.x - 0.05f) mainSpriteRenderer.flipX = false; // moving left
        }
    }

    protected virtual void Wander()
    {
        // Periodically check for puddles if we don't have one
        if (targetPuddle == null)
        {
            FindNearbyPuddle();
        }
        else
        {
            // Puddle might have dried up while walking
            if (!targetPuddle.gameObject.activeInHierarchy || targetPuddle.currentSize <= 0)
            {
                targetPuddle = null;
                ShowBubble(null); // Lost puddle, hide exclamation mark
                PickNewWanderTarget();
            }
            else
            {
                targetPosition = targetPuddle.transform.position;
            }
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, walkSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            if (targetPuddle != null)
            {
                // Reached the puddle manually (in case trigger physics didn't fire)
                StepInPuddle(targetPuddle);
                targetPuddle = null;
            }
            else
            {
                PickNewWanderTarget();
            }
        }
    }

    protected virtual void FindNearbyPuddle()
    {
        int layerMask = puddleLayer.value == 0 ? Physics2D.AllLayers : puddleLayer.value;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, puddleDetectionRadius, layerMask);
        float closestDist = float.MaxValue;
        Puddle closestPuddle = null;

        foreach (var hit in hits)
        {
            Puddle p = hit.GetComponent<Puddle>();
            if (p != null && p.currentSize > 0)
            {
                float d = Vector2.Distance(transform.position, p.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    closestPuddle = p;
                }
            }
        }

        if (closestPuddle != null)
        {
            if (targetPuddle != closestPuddle) // Just found a new puddle!
            {
                ShowBubble(bubbleFoundPuddle);
                if (anim != null) anim.SetTrigger(animTriggerFoundPuddle);
            }

            targetPuddle = closestPuddle;
            targetPosition = targetPuddle.transform.position;
        }
    }

    protected virtual void RunOffMap()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);
        
        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            Debug.Log($"{gameObject.name} left the map limits and vanished.");
            Destroy(gameObject);
        }
    }

    protected virtual void PickNewWanderTarget()
    {
        if (GameManager.Instance == null) return;
        Bounds bounds = GameManager.Instance.mapBounds;
        float rx = Random.Range(bounds.min.x, bounds.max.x);
        float ry = Random.Range(bounds.min.y, bounds.max.y);
        targetPosition = new Vector2(rx, ry);
    }

    protected virtual void SetEdgeTarget()
    {
        if (GameManager.Instance == null) return;
        Bounds bounds = GameManager.Instance.mapBounds;
        // Find nearest horizontal edge to run away
        float dirX = transform.position.x > 0 ? bounds.max.x + 5f : bounds.min.x - 5f;
        targetPosition = new Vector2(dirX, transform.position.y);
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignore interactions if we are already leaving
        if (currentState != NPCState.Wandering) return;

        // 1. Got hit by a falling raindrop
        if (collision.GetComponent<Raindrop>() != null)
        {
            GetWet();
        }
        
        // 2. Stepped on a puddle
        Puddle puddle = collision.GetComponent<Puddle>();
        if (puddle != null)
        {
            StepInPuddle(puddle);
        }
    }

    protected virtual void GetWet()
    {
        Debug.Log($"{gameObject.name} was rained on! Global mood penalties applied.");
        currentState = NPCState.Fleeing;
        
        ShowBubble(bubbleSad);
        if (anim != null)
        {
            anim.SetBool(animParamIsRunning, true);
            anim.SetTrigger(animTriggerSadLeave);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.ModifyMood(rainMoodPenalty);
            
        SetEdgeTarget();
    }

    protected virtual void StepInPuddle(Puddle puddle)
    {
        if (currentState == NPCState.PlayingInWater || currentState == NPCState.Satisfied || currentState == NPCState.Fleeing) return;

        Debug.Log($"{gameObject.name} splashed in a puddle! Feeling satisfied.");
        currentState = NPCState.PlayingInWater;
        
        // Shrink puddle
        puddle.ModifySize(-puddleShrinkAmount);

        // Boost global mood and register satisfied score
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ModifyMood(puddleMoodBonus);
            GameManager.Instance.RegisterNPCSatisfied();
        }

        if (AudioManager.Instance != null && puddleSplashSound != null)
        {
            AudioManager.Instance.PlaySFXRandomPitch(puddleSplashSound, 0.85f, 1.15f);
        }
            
        StartCoroutine(PlayInWaterRoutine());
    }

    protected virtual System.Collections.IEnumerator PlayInWaterRoutine()
    {
        ShowBubble(bubbleHappy);
        if (anim != null) anim.SetTrigger(animTriggerPlayWater);

        float timer = 0f;
        Vector3 origPos = spriteVisual != null ? spriteVisual.localPosition : Vector3.zero;

        // Manual bounce effect if no animator
        while (timer < playTime)
        {
            timer += Time.deltaTime;
            if (anim == null && spriteVisual != null)
            {
                float bounce = Mathf.Abs(Mathf.Sin(timer * 15f)) * 0.5f;
                spriteVisual.localPosition = origPos + Vector3.up * bounce;
            }
            yield return null;
        }

        // Reset visual position
        if (anim == null && spriteVisual != null)
        {
            spriteVisual.localPosition = origPos;
        }

        // Ready to leave
        if (anim != null) 
        {
            anim.SetBool(animParamIsRunning, false);
            anim.SetTrigger(animTriggerHappyLeave);
        }
        
        currentState = NPCState.Satisfied;
        SetEdgeTarget();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Visualize the puddle detection radius in the Editor
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Cyan with some transparency
        Gizmos.DrawWireSphere(transform.position, puddleDetectionRadius);
    }
}
