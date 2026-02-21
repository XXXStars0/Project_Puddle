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
    [Tooltip("Input Manager Button for Rain (e.g., 'Jump' maps to Space/Gamepad A)")]
    public string rainButton = "Jump";

    [Header("Size Settings")]
    public float maxSize = 100f;
    public float minSize = 20f;
    public float currentSize = 100f;
    public float rainCostPerSecond = 15f; // How much size is lost per second of raining

    [Header("Visuals & Environment")]
    public GameObject rainPrefab; // The raindrop prefab to spawn
    public float rainSpawnRate = 10f; // Raindrops per second
    [Tooltip("2.5D空间中，云距离地面的视觉高度 (阴影的Y轴偏移量)")]
    public float shadowOffset = 5f; 
    public GameObject shadowPrefab; // Optional: A sprite dropping a shadow on the ground

    private bool isRaining = false;
    private Vector3 initialScale;
    private float nextRainSpawnTime = 0f;
    private float currentGroundY = float.MinValue; 
    private GameObject currentShadow;

    void Start()
    {
        initialScale = transform.localScale;
        currentSize = maxSize;
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
        // TODO: Play rain sound, trigger particle emission or instantiate continuous rain prefab
    }

    private void StopRain()
    {
        isRaining = false;
        Debug.Log("Stopped raining.");
        // TODO: Stop rain sound, halt particle emission
    }

    /// <summary>
    /// Updates the size of the cloud and its rain width based on currentSize.
    /// </summary>
    private void UpdateVisuals()
    {
        // Scale the cloud texture/transform proportionally to its current size
        float scaleMultiplier = currentSize / maxSize;
        transform.localScale = initialScale * scaleMultiplier;
        
        // Shadow visual scaling
        if (currentShadow != null)
        {
            currentShadow.transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, 1f);
        }
    }

    /// <summary>
    /// Updates the shadow projection based on a 2.5D fixed altitude.
    /// </summary>
    private void UpdateShadow()
    {
        // 在 2.5D 俯视角地图中，"地面" 是整个背景空间。阴影只需保持固定的 Y 轴视觉高度差即可产生立体感。
        currentGroundY = transform.position.y - shadowOffset;
        
        if (currentShadow != null)
        {
            // 同步阴影到计算出的地面落点
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
        
        switch (powerUpType)
        {
            case "Water":
                currentSize = Mathf.Min(currentSize + amount, maxSize);
                UpdateVisuals();
                break;
            case "Speed":
                // Temporary speed boost logic interface
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
}
