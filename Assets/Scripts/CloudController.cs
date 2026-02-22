using UnityEngine;

public class CloudController : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 10f;
    public float acceleration = 25f;
    public float deceleration = 15f;
    private Vector2 currentVelocity;
    
    [Header("Inputs")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";
    public string rainButton = "Jump";
    public string lowerHeightButton = "LowerHeight";
    public string raiseHeightButton = "RaiseHeight";

    [Header("Size")]
    public float maxSize = 100f;
    public float minSize = 20f;
    public float currentSize = 100f;
    public float rainCostPerSecond = 15f; 

    [Header("Visuals")]
    public GameObject rainPrefab; 
    public float rainSpawnRate = 10f; 
    public float cloudHeightAboveGround = 5f;
    public float minCloudHeight = 2f;
    public float maxCloudHeight = 12f;
    public float heightAdjustSpeed = 4f;
    public GameObject shadowPrefab; 

    [Header("Audio")]
    public AudioClip powerUpSound;
    public AudioSource rainLoopSource; 

    [Header("Speed Boost")]
    public float speedBoostMaxSpeedMultiplier = 1.2f;
    public float speedBoostAccelMultiplier = 1.5f;
    public float speedBoostDecelMultiplier = 4.0f;

    private float baseMaxSpeed;
    private float baseAcceleration;
    private float baseDeceleration;
    private float speedBoostEndTime = 0f;

    private bool isRaining = false;
    private Vector3 initialScale;
    private float nextRainSpawnTime = 0f;
    private float currentGroundY = float.MinValue; 
    private GameObject currentShadow;

    private Coroutine rainFadeCoroutine;
    private float defaultRainVolume = 1f;
    private float visualBumpMultiplier = 1f;
    private Coroutine bumpCoroutine;
    private Coroutine speedBoostCoroutine;
    private SpriteRenderer cloudSprite;

    void Start()
    {
        baseMaxSpeed = maxSpeed;
        baseAcceleration = acceleration;
        baseDeceleration = deceleration;

        cloudSprite = GetComponentInChildren<SpriteRenderer>();
        initialScale = transform.localScale;
        currentSize = maxSize;
        cloudHeightAboveGround = Mathf.Clamp(cloudHeightAboveGround, minCloudHeight, maxCloudHeight);
        UpdateShadow();

        if (shadowPrefab != null)
        {
            currentShadow = Instantiate(shadowPrefab, new Vector3(transform.position.x, currentGroundY, 0), Quaternion.identity);
        }

        if (rainLoopSource != null)
        {
            defaultRainVolume = rainLoopSource.volume;
            rainLoopSource.Stop();
        }

        // Debug.Log("Cloud initialized.");
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        UpdateShadow();
        HandleMovement();
        HandleHeightAdjust(); 
        HandleRain();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw(horizontalAxis);
        float v = Input.GetAxisRaw(verticalAxis);
        
        Vector2 inputVector = new Vector2(h, v);
        
        if (inputVector.magnitude > 1f)
        {
            inputVector.Normalize();
        }

        Vector2 targetVelocity = inputVector * maxSpeed;

        if (inputVector != Vector2.zero)
        {
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, deceleration * Time.deltaTime);
        }

        transform.position += (Vector3)currentVelocity * Time.deltaTime;

        if (GameManager.Instance != null && GameManager.Instance.mapBounds.size != Vector3.zero)
        {
            Bounds bounds = GameManager.Instance.mapBounds;
            Vector3 pos = transform.position;
            
            bool hitWall = false;

            if (pos.x < bounds.min.x) { pos.x = bounds.min.x; currentVelocity.x = 0; hitWall = true; }
            if (pos.x > bounds.max.x) { pos.x = bounds.max.x; currentVelocity.x = 0; hitWall = true; }
            if (pos.y < bounds.min.y) { pos.y = bounds.min.y; currentVelocity.y = 0; hitWall = true; }
            if (pos.y > bounds.max.y) { pos.y = bounds.max.y; currentVelocity.y = 0; hitWall = true; }

            if (hitWall) transform.position = pos;
        }
    }

    private void HandleRain()
    {
        if (Input.GetButtonDown(rainButton))
        {
            if (currentSize > minSize) StartRain();
            // else Debug.Log("Cloud too small!");
        }
        
        if (Input.GetButtonUp(rainButton) && isRaining) StopRain();

        if (isRaining)
        {
            currentSize -= rainCostPerSecond * Time.deltaTime;
            currentSize = Mathf.Max(currentSize, minSize);
            
            UpdateVisuals();
            SpawnRainEffect(); 

            if (currentSize <= minSize)
            {
                // Debug.Log("Raining stopped forcibly.");
                StopRain();
            }
        }
    }

    private void StartRain()
    {
        isRaining = true;
        // Debug.Log("Started raining.");
        
        if (rainLoopSource != null)
        {
            if (rainFadeCoroutine != null)
            {
                StopCoroutine(rainFadeCoroutine);
                rainFadeCoroutine = null;
            }
            
            rainLoopSource.volume = defaultRainVolume;
            if (!rainLoopSource.isPlaying) rainLoopSource.Play();
        }
    }

    private void StopRain()
    {
        isRaining = false;
        // Debug.Log("Stopped raining.");
        
        if (rainLoopSource != null && rainLoopSource.gameObject.activeInHierarchy)
        {
            if (rainFadeCoroutine != null) StopCoroutine(rainFadeCoroutine);
            rainFadeCoroutine = StartCoroutine(FadeOutRainAudio());
        }
    }

    private System.Collections.IEnumerator FadeOutRainAudio()
    {
        float fadeDuration = 0.5f;
        float startVol = rainLoopSource.volume;
        float t = 0f;

        while (t < fadeDuration)
        {
            if (rainLoopSource == null) yield break;
            t += Time.deltaTime;
            rainLoopSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
            yield return null;
        }

        if (rainLoopSource != null)
        {
            rainLoopSource.Stop();
            rainLoopSource.volume = defaultRainVolume;
        }

        rainFadeCoroutine = null;
    }

    private void UpdateVisuals()
    {
        float scaleMultiplier = (currentSize / maxSize) * visualBumpMultiplier;
        transform.localScale = initialScale * scaleMultiplier;
        
        if (currentShadow != null)
        {
            float stableMultiplier = currentSize / maxSize;
            currentShadow.transform.localScale = new Vector3(stableMultiplier, stableMultiplier, 1f);
        }
    }

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
            isLowering = Input.GetKey(KeyCode.J);
            isRaising = Input.GetKey(KeyCode.K);
        }

        if (isLowering)
        {
            cloudHeightAboveGround -= delta;
            cloudHeightAboveGround = Mathf.Max(cloudHeightAboveGround, minCloudHeight);
            transform.position = new Vector3(transform.position.x, currentGroundY + cloudHeightAboveGround, transform.position.z);
        }
        if (isRaising)
        {
            cloudHeightAboveGround += delta;
            cloudHeightAboveGround = Mathf.Min(cloudHeightAboveGround, maxCloudHeight);
            transform.position = new Vector3(transform.position.x, currentGroundY + cloudHeightAboveGround, transform.position.z);
        }
    }

    private void UpdateShadow()
    {
        currentGroundY = transform.position.y - cloudHeightAboveGround;
        if (currentShadow != null)
        {
            currentShadow.transform.position = new Vector3(transform.position.x, currentGroundY, 0);
        }
    }

    private void SpawnRainEffect()
    {
        if (rainPrefab == null) return;

        if (Time.time >= nextRainSpawnTime)
        {
            float sizeRatio = currentSize / maxSize;
            float rainWidth = (initialScale.x * sizeRatio) / 2f;
            int dropsToSpawn = Mathf.Max(1, Mathf.RoundToInt(sizeRatio * 4f));

            for (int i = 0; i < dropsToSpawn; i++)
            {
                float randomX = Random.Range(-rainWidth, rainWidth);
                Vector3 spawnPos = transform.position + new Vector3(randomX, -0.5f, 0);
                GameObject raindropObj = Instantiate(rainPrefab, spawnPos, Quaternion.identity);
                Raindrop dropScript = raindropObj.GetComponent<Raindrop>();
                if (dropScript != null) dropScript.SetTargetGroundY(currentGroundY);
            }

            nextRainSpawnTime = Time.time + (1f / rainSpawnRate);
        }
    }

    public void CollectPowerUp(string powerUpType, float amount)
    {
        // Debug.Log($"Collected: {powerUpType}");
        
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
                speedBoostEndTime = Time.time + amount;
                if (speedBoostCoroutine == null) speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine());
                break;
            case "DarkCloud":
                maxSize += amount; 
                currentSize += amount; 
                UpdateVisuals();
                break;
            default:
                // Debug.LogWarning($"Unknown power-up: {powerUpType}");
                break;
        }
    }

    public void ApplyEnvironmentalEffect(Vector2 force)
    {
        // Debug.Log($"Force applied: {force}");
    }

    private System.Collections.IEnumerator CollectionBumpRoutine()
    {
        float t = 0f;
        float duration = 0.3f; 
        while (t < duration)
        {
            t += Time.deltaTime;
            visualBumpMultiplier = 1f + Mathf.Sin((t / duration) * Mathf.PI) * 0.3f;
            UpdateVisuals();
            yield return null;
        }
        visualBumpMultiplier = 1f;
        UpdateVisuals();
    }

    private System.Collections.IEnumerator SpeedBoostRoutine()
    {
        maxSpeed = baseMaxSpeed * speedBoostMaxSpeedMultiplier;
        acceleration = baseAcceleration * speedBoostAccelMultiplier;
        deceleration = baseDeceleration * speedBoostDecelMultiplier; 

        if (AudioManager.Instance != null) AudioManager.Instance.SetSpeedBGMState(true);

        while (Time.time < speedBoostEndTime)
        {
            if (cloudSprite != null)
            {
                float flash = Mathf.PingPong(Time.time * 10f, 1f);
                cloudSprite.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.015f), flash);
            }
            yield return null;
        }

        maxSpeed = baseMaxSpeed;
        acceleration = baseAcceleration;
        deceleration = baseDeceleration;
        if (cloudSprite != null) cloudSprite.color = Color.white;
        
        if (AudioManager.Instance != null) AudioManager.Instance.SetSpeedBGMState(false);
        speedBoostCoroutine = null;
    }
}
