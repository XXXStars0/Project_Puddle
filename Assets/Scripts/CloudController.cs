using UnityEngine;

/// <summary>
/// Controls the behavior of the 2D Cloud character.
/// </summary>
public class CloudController : MonoBehaviour
{
    [Header("Movement Settings (Momentum)")]
    public float maxSpeed = 10f;
    public float acceleration = 25f;
    public float deceleration = 15f;
    private Vector2 currentVelocity;
    
    [Header("Control Bindings")]
    [Tooltip("Input Manager Axis for Left/Right (e.g., 'Horizontal')")]
    public string horizontalAxis = "Horizontal";
    [Tooltip("Input Manager Axis for Up/Down (e.g., 'Vertical')")]
    public string verticalAxis = "Vertical";
    [Tooltip("Input Manager Button for Rain (e.g. 'Jump'/'Submit' maps to Space/Gamepad A). Can map standard Xbox A here.")]
    public string rainButton = "Jump";
    [Tooltip("Input Manager Button to lower cloud. Setup as 'LowerHeight' mapped to J or joystick button 4 (LB).")]
    public string lowerHeightButton = "LowerHeight";
    [Tooltip("Input Manager Button to raise cloud. Setup as 'RaiseHeight' mapped to K or joystick button 5 (RB).")]
    public string raiseHeightButton = "RaiseHeight";

    [Header("Size Settings")]
    public float maxSize = 100f;
    public float minSize = 20f;
    public float currentSize = 100f;
    public float rainCostPerSecond = 15f; // How much size is lost per second of raining

    [Header("Visuals & Environment")]
    public GameObject rainPrefab; // The raindrop prefab to spawn
    public float rainSpawnRate = 10f; // Raindrops per second
    [Tooltip("Cloud height above ground (shadow). Distance between cloud and shadow. J/K adjust this; shadow stays fixed.")]
    public float cloudHeightAboveGround = 5f;
    [Tooltip("Minimum cloud height above ground (closest to shadow).")]
    public float minCloudHeight = 2f;
    [Tooltip("Maximum cloud height above ground (farthest from shadow).")]
    public float maxCloudHeight = 12f;
    [Tooltip("Units per second when holding J/K to change height.")]
    public float heightAdjustSpeed = 4f;
    public GameObject shadowPrefab; // Optional: A sprite dropping a shadow on the ground

    [Header("Audio")]
    public AudioClip powerUpSound;
    public AudioSource rainLoopSource; // Attach an AudioSource for continuous raining

    private bool isRaining = false;
    private Vector3 initialScale;
    private float nextRainSpawnTime = 0f;
    private float currentGroundY = float.MinValue; 
    private GameObject currentShadow;

    // Visual feedback internals
    private float visualBumpMultiplier = 1f;
    private Coroutine bumpCoroutine;
    private Coroutine speedBoostCoroutine;
    private SpriteRenderer cloudSprite;

    void Start()
    {
        cloudSprite = GetComponentInChildren<SpriteRenderer>();
        initialScale = transform.localScale;
        currentSize = maxSize;
        cloudHeightAboveGround = Mathf.Clamp(cloudHeightAboveGround, minCloudHeight, maxCloudHeight);
        UpdateShadow(); // Calculate ground once at start or every frame if ground height varies

        if (shadowPrefab != null)
        {
            currentShadow = Instantiate(shadowPrefab, new Vector3(transform.position.x, currentGroundY, 0), Quaternion.identity);
        }

        Debug.Log("Cloud initialized. Ready to move and rain.");
    }

    void Update()
    {
        // Prevent actions when game state is not explicitly set to Playing (e.g. Menu, Paused, GameOver)
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        UpdateShadow(); // Keep shadow/ground level updated as we move
        HandleMovement();
        HandleHeightAdjust(); // J/K: move cloud up/down relative to shadow (shadow stays fixed)
        HandleRain();
    }

    /// <summary>
    /// Handles 2D movement based on configured control bindings.
    /// Prevents cloud from moving below the ground level.
    /// </summary>
    private void HandleMovement()
    {
        // Read from standard Unity Input Manager (supports Gamepad Thumbsticks & D-Pad, Arrow Keys, WASD)
        float h = Input.GetAxisRaw(horizontalAxis);
        float v = Input.GetAxisRaw(verticalAxis);
        
        Vector2 inputVector = new Vector2(h, v);
        
        // Clamp magnitude to 1 to prevent diagonal speed boost
        if (inputVector.magnitude > 1f)
        {
            inputVector.Normalize();
        }

        Vector2 targetVelocity = inputVector * maxSpeed;

        // Apply custom inertia/momentum
        if (inputVector != Vector2.zero)
        {
            // Accelerating
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            // Decelerating (Braking)
            currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, deceleration * Time.deltaTime);
        }

        transform.position += (Vector3)currentVelocity * Time.deltaTime;

        // Smart Bounds Clamping & Momentum Absorb
        if (GameManager.Instance != null && GameManager.Instance.mapBounds.size != Vector3.zero)
        {
            Bounds bounds = GameManager.Instance.mapBounds;
            Vector3 pos = transform.position;
            
            bool hitWall = false;

            if (pos.x < bounds.min.x) { pos.x = bounds.min.x; currentVelocity.x = 0; hitWall = true; }
            if (pos.x > bounds.max.x) { pos.x = bounds.max.x; currentVelocity.x = 0; hitWall = true; }
            if (pos.y < bounds.min.y) { pos.y = bounds.min.y; currentVelocity.y = 0; hitWall = true; }
            if (pos.y > bounds.max.y) { pos.y = bounds.max.y; currentVelocity.y = 0; hitWall = true; }

            if (hitWall)
            {
                transform.position = pos;
            }
        }
    }

    /// <summary>
    /// Handles the raining input, mechanics, and size constraints.
    /// </summary>
    private void HandleRain()
    {
        // Check input for starting rain
        if (Input.GetButtonDown(rainButton))
        {
            if (currentSize > minSize)
            {
                StartRain();
            }
            else
            {
                Debug.Log("Cloud is too small to start raining!");
            }
        }
        
        // Check input for stopping rain
        if (Input.GetButtonUp(rainButton) && isRaining)
        {
            StopRain();
        }

        // Handle raining logic over time
        if (isRaining)
        {
            currentSize -= rainCostPerSecond * Time.deltaTime;
            currentSize = Mathf.Max(currentSize, minSize);
            
            UpdateVisuals();
            SpawnRainEffect(); // Called every frame while raining

            // Automatically stop if we hit the minimum size
            if (currentSize <= minSize)
            {
                Debug.Log("Cloud reached minimum size. Raining stopped forcibly.");
                StopRain();
            }
        }
    }

    private void StartRain()
    {
        isRaining = true;
        Debug.Log("Started raining.");
        
        if (rainLoopSource != null && !rainLoopSource.isPlaying)
        {
            rainLoopSource.Play();
        }
        
        // TODO: trigger particle emission or instantiate continuous rain prefab
    }

    private void StopRain()
    {
        isRaining = false;
        Debug.Log("Stopped raining.");
        
        if (rainLoopSource != null && rainLoopSource.isPlaying)
        {
            rainLoopSource.Stop();
        }

        // TODO: halt particle emission
    }

    /// <summary>
    /// Updates the size of the cloud and its rain width based on currentSize.
    /// </summary>
    private void UpdateVisuals()
    {
        // Scale the cloud texture/transform proportionally to its current size, multiplied by bump feedback
        float scaleMultiplier = (currentSize / maxSize) * visualBumpMultiplier;
        transform.localScale = initialScale * scaleMultiplier;
        
        // Shadow visual scaling (using stable base size, excluding bump bounce)
        if (currentShadow != null)
        {
            float stableMultiplier = currentSize / maxSize;
            currentShadow.transform.localScale = new Vector3(stableMultiplier, stableMultiplier, 1f);
        }
    }

    /// <summary>
    /// Adjusts cloud height above ground with J/K. Shadow stays fixed; only the cloud moves vertically.
    /// </summary>
    private void HandleHeightAdjust()
    {
        float delta = heightAdjustSpeed * Time.deltaTime;
        
        bool isLowering = false;
        bool isRaising = false;
        
        try 
        { 
            isLowering = Input.GetButton(lowerHeightButton); 
            isRaising = Input.GetButton(raiseHeightButton); 
        } 
        catch 
        { 
            // Fallback to old Keys if Input Manager is not configured yet
            isLowering = Input.GetKey(KeyCode.J);
            isRaising = Input.GetKey(KeyCode.K);
        }

        if (isLowering)
        {
            cloudHeightAboveGround -= delta;
            cloudHeightAboveGround = Mathf.Max(cloudHeightAboveGround, minCloudHeight);
            // Move cloud down so it gets closer to shadow; shadow Y (currentGroundY) unchanged
            transform.position = new Vector3(transform.position.x, currentGroundY + cloudHeightAboveGround, transform.position.z);
        }
        if (isRaising)
        {
            cloudHeightAboveGround += delta;
            cloudHeightAboveGround = Mathf.Min(cloudHeightAboveGround, maxCloudHeight);
            // Move cloud up; shadow Y (currentGroundY) unchanged
            transform.position = new Vector3(transform.position.x, currentGroundY + cloudHeightAboveGround, transform.position.z);
        }
    }

    /// <summary>
    /// Updates the shadow projection based on cloud height above ground (2.5D fixed altitude).
    /// Shadow sits at cloud Y minus cloudHeightAboveGround.
    /// </summary>
    private void UpdateShadow()
    {
        // In 2.5D top-down view, "ground" is the shadow plane. Keep shadow at fixed visual offset below cloud.
        currentGroundY = transform.position.y - cloudHeightAboveGround;

        if (currentShadow != null)
        {
            currentShadow.transform.position = new Vector3(transform.position.x, currentGroundY, 0);
        }
    }

    /// <summary>
    /// Interface for spawning rain prefabs (e.g., individual raindrops) or updating a rain particle system.
    /// </summary>
    private void SpawnRainEffect()
    {
        if (rainPrefab == null) return;

        if (Time.time >= nextRainSpawnTime)
        {
            // Calculate spawn position (could randomize x based on cloud width)
            float rainWidth = (initialScale.x * (currentSize / maxSize)) / 2f;
            float randomX = Random.Range(-rainWidth, rainWidth);
            Vector3 spawnPos = transform.position + new Vector3(randomX, -0.5f, 0);

            GameObject raindropObj = Instantiate(rainPrefab, spawnPos, Quaternion.identity);
            
            // If the raindrop has our script, tell it where the ground is for the 2.5D effect
            Raindrop dropScript = raindropObj.GetComponent<Raindrop>();
            if (dropScript != null)
            {
                dropScript.SetTargetGroundY(currentGroundY);
            }

            nextRainSpawnTime = Time.time + (1f / rainSpawnRate);
        }
    }

    /// <summary>
    /// Interface for picking up power-ups (e.g., restoring water/size, modifying speed).
    /// </summary>
    /// <param name="powerUpType">A string identifier for the power-up type.</param>
    /// <param name="amount">The value associated with the power-up.</param>
    public void CollectPowerUp(string powerUpType, float amount)
    {
        Debug.Log($"Collected power-up: {powerUpType} ({amount})");
        
        if (AudioManager.Instance != null && powerUpSound != null)
        {
            AudioManager.Instance.PlaySFXRandomPitch(powerUpSound, 0.9f, 1.1f);
        }
        
        if (bumpCoroutine != null) StopCoroutine(bumpCoroutine);
        bumpCoroutine = StartCoroutine(CollectionBumpRoutine());
        
        switch (powerUpType)
        {
            case "Water":
                currentSize = Mathf.Min(currentSize + amount, maxSize);
                UpdateVisuals();
                break;
            case "Speed":
                if (speedBoostCoroutine != null) StopCoroutine(speedBoostCoroutine);
                speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(amount));
                break;
            case "DarkCloud":
                maxSize += amount; // Increases maximum rain capacity
                currentSize += amount; // Dark cloud immediately gives weather boost
                UpdateVisuals();
                break;
            // Leave open for new features
            default:
                Debug.LogWarning($"Unknown power-up type: {powerUpType}");
                break;
        }
    }

    /// <summary>
    /// Future interface: Custom feature extension point.
    /// Can be used for environmental interactions (e.g., wind blow).
    /// </summary>
    public void ApplyEnvironmentalEffect(Vector2 force)
    {
        Debug.Log($"Applied environmental force: {force}");
        // TODO: Add external forces to movement or shape
    }

    // --- Feedback & Power-up Coroutines ---
    
    private System.Collections.IEnumerator CollectionBumpRoutine()
    {
        float t = 0f;
        float duration = 0.3f; // Fast elastic bump
        while (t < duration)
        {
            t += Time.deltaTime;
            // Creates a bounce multiplier going 1.0 -> 1.3 -> 1.0
            visualBumpMultiplier = 1f + Mathf.Sin((t / duration) * Mathf.PI) * 0.3f;
            UpdateVisuals();
            yield return null;
        }
        visualBumpMultiplier = 1f;
        UpdateVisuals();
    }

    private System.Collections.IEnumerator SpeedBoostRoutine(float duration)
    {
        float originalMaxSpeed = maxSpeed;
        float originalAccel = acceleration;
        float originalDecel = deceleration;

        // Boost speed and make handling much snappier (reduce slippery inertia)
        maxSpeed = originalMaxSpeed * 1.5f;
        acceleration = originalAccel * 4f; 
        deceleration = originalDecel * 4f; 

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Flashing glow effect ping-ponging between white and a bright energetic cyan
            if (cloudSprite != null)
            {
                float flash = Mathf.PingPong(Time.time * 10f, 1f);
                cloudSprite.color = Color.Lerp(Color.white, new Color(0.6f, 1f, 1f), flash);
            }
            yield return null;
        }

        // Restore handling attributes natively upon expiration
        maxSpeed = originalMaxSpeed;
        acceleration = originalAccel;
        deceleration = originalDecel;
        if (cloudSprite != null) cloudSprite.color = Color.white;
    }
}
