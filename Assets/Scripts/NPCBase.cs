using UnityEngine;

public class NPCBase : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;

    [Header("Mood")]
    public float rainMoodPenalty = -10f;
    public float puddleMoodBonus = 15f;
    
    [Header("AI")]
    public float puddleShrinkAmount = 0.5f; 
    public float puddleDetectionRadius = 4f; 
    public LayerMask puddleLayer; 

    [Header("Bubbles")]
    public GameObject bubbleFoundPuddle; 
    public GameObject bubbleHappy; 
    public GameObject bubbleSad; 

    [Header("Audio")]
    public AudioClip soundFoundPuddle;
    public AudioClip soundHappy;
    public AudioClip soundSad;
    public AudioClip puddleSplashSound; 

    [Header("Visuals")]
    public SpriteRenderer mainSpriteRenderer;
    public GameObject puddleSplashPrefab; 
    public Sprite[] walkSprites; 
    public Sprite[] jumpSprites; 
    public float walkAnimSpeed = 0.25f;
    public float runAnimSpeed = 0.12f;
    public float jumpAnimSpeed = 0.3f;
    
    private float animTimer = 0f;
    private int currentFrame = 0;

    public Animator anim;
    public Transform spriteVisual;
    public float playTime = 1.0f; 
    
    public string animParamIsRunning = "IsRunning";
    public string animTriggerFoundPuddle = "FoundPuddle";
    public string animTriggerPlayWater = "PlayWater";
    public string animTriggerHappyLeave = "HappyLeave";
    public string animTriggerSadLeave = "SadLeave";

    protected enum NPCState { Wandering, Searching, PlayingInWater, Fleeing, Satisfied }
    protected NPCState currentState = NPCState.Wandering;

    protected Vector2 targetPosition;
    protected Puddle targetPuddle;

    protected virtual void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterNPCSpawn();
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.isKinematic = true;

        ShowBubble(null);
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
        if (GameManager.Instance != null && GameManager.Instance.mapBounds.size != Vector3.zero)
        {
            Bounds cullBounds = GameManager.Instance.mapBounds;
            cullBounds.Expand(10f); 
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
            case NPCState.Searching:
            case NPCState.PlayingInWater:
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
            case NPCState.Searching:
                currentSpeed = walkAnimSpeed * 2f; 
                currentArray = walkSprites; 
                break;
            case NPCState.PlayingInWater:
                currentSpeed = jumpAnimSpeed;
                currentArray = jumpSprites;
                break;
            case NPCState.Fleeing:
            case NPCState.Satisfied:
                currentSpeed = runAnimSpeed; 
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

        if (currentState != NPCState.PlayingInWater)
        {
            if (targetPosition.x > transform.position.x + 0.05f) mainSpriteRenderer.flipX = true;  
            else if (targetPosition.x < transform.position.x - 0.05f) mainSpriteRenderer.flipX = false; 
        }
    }

    protected virtual void Wander()
    {
        if (targetPuddle != null)
        {
            if (!targetPuddle.gameObject.activeInHierarchy || targetPuddle.currentSize <= 0)
            {
                targetPuddle = null;
                ShowBubble(null); 
                PickNewWanderTarget();
            }
        }

        if (targetPuddle == null)
        {
            if (bubbleFoundPuddle != null && bubbleFoundPuddle.activeSelf)
            {
                ShowBubble(null);
            }
            FindNearbyPuddle();
        }
        else
        {
            targetPosition = targetPuddle.transform.position;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, walkSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            if (targetPuddle != null)
            {
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, puddleDetectionRadius);
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
            if (targetPuddle != closestPuddle) 
            {
                targetPuddle = closestPuddle;
                targetPosition = targetPuddle.transform.position;
                StartCoroutine(ReactToPuddleRoutine());
            }
        }
    }

    private System.Collections.IEnumerator ReactToPuddleRoutine()
    {
        currentState = NPCState.Searching; 
        ShowBubble(bubbleFoundPuddle);
        if (anim != null) anim.SetTrigger(animTriggerFoundPuddle);
        
        float timer = 0f;
        while (timer < 0.6f)
        {
            if (targetPuddle == null || !targetPuddle.gameObject.activeInHierarchy || targetPuddle.currentSize <= 0)
            {
                break;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (currentState == NPCState.Searching)
        {
            currentState = NPCState.Wandering; 
            if (targetPuddle == null || !targetPuddle.gameObject.activeInHierarchy || targetPuddle.currentSize <= 0)
            {
                targetPuddle = null;
                ShowBubble(null);
                PickNewWanderTarget();
            }
        }
    }

    protected virtual void RunOffMap()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            // Debug.Log($"{gameObject.name} left.");
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
        float dirX = transform.position.x > 0 ? bounds.max.x + 5f : bounds.min.x - 5f;
        targetPosition = new Vector2(dirX, transform.position.y);
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (currentState != NPCState.Wandering) return;

        if (collision.GetComponent<Raindrop>() != null)
        {
            GetWet();
        }
        
        Puddle puddle = collision.GetComponent<Puddle>();
        if (puddle != null)
        {
            StepInPuddle(puddle);
        }
    }

    protected virtual void GetWet()
    {
        // Debug.Log($"{gameObject.name} wet.");
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

        // Debug.Log($"{gameObject.name} splash.");
        currentState = NPCState.PlayingInWater;
        
        puddle.ModifySize(-puddleShrinkAmount);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ModifyMood(puddleMoodBonus);
            GameManager.Instance.RegisterNPCSatisfied();
        }

        if (AudioManager.Instance != null && puddleSplashSound != null)
        {
            AudioManager.Instance.PlaySFXRandomPitch(puddleSplashSound, 0.85f, 1.15f);
        }

        if (puddleSplashPrefab != null)
        {
            Instantiate(puddleSplashPrefab, new Vector3(transform.position.x, transform.position.y - 0.2f, 0f), Quaternion.identity);
        }
            
        StartCoroutine(PlayInWaterRoutine());
    }

    protected virtual System.Collections.IEnumerator PlayInWaterRoutine()
    {
        ShowBubble(bubbleHappy);
        if (anim != null) anim.SetTrigger(animTriggerPlayWater);

        float timer = 0f;
        Vector3 origPos = spriteVisual != null ? spriteVisual.localPosition : Vector3.zero;

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

        if (anim == null && spriteVisual != null)
        {
            spriteVisual.localPosition = origPos;
        }

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
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f); 
        Gizmos.DrawWireSphere(transform.position, puddleDetectionRadius);
    }
}
