using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType
    {
        Water, 
        Speed  
    }

    private enum PowerUpState
    {
        Spawning,
        Idle,
        Flashing,
        Despawning
    }

    [Header("Core")]
    public PowerUpType type = PowerUpType.Water;
    public float amount = 30f; 

    [Header("Timings")]
    public float lifetime = 8f;            
    public float spawnDuration = 0.5f;     
    public float flashDuration = 2.0f;     
    public float despawnDuration = 0.5f;   

    [Header("Visuals")]
    public Transform spriteTransform;
    public float floatAmplitude = 0.15f;    
    public float floatSpeed = 3f;          
    public Vector3 moveDirection = Vector3.zero; 
    public float moveVelocity = 0f;

    [Header("Effects")]
    public AudioClip spawnSound;
    public GameObject collectEffect; 

    private PowerUpState state = PowerUpState.Spawning;
    private float stateTimer = 0f;
    private float totalAliveTime = 0f;
    private Vector3 initialScale;
    private Vector3 basePosition;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        if (spriteTransform == null && transform.childCount > 0) 
        {
            spriteTransform = transform.GetChild(0);
        }

        if (spriteTransform != null)
        {
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
            initialScale = spriteTransform.localScale;
            spriteTransform.localScale = Vector3.zero; 
        }
        else 
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            initialScale = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        basePosition = transform.position;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

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
        float floatOffset = Mathf.Sin(totalAliveTime * floatSpeed) * floatAmplitude;
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
                float t = Mathf.Clamp01(stateTimer / spawnDuration);
                float elasticT = ElasticOut(t);
                ScaleTarget(initialScale * elasticT);
                if (t >= 1f) ChangeState(PowerUpState.Idle);
                break;

            case PowerUpState.Idle:
                if (timeRemaining <= (flashDuration + despawnDuration) && timeRemaining > despawnDuration)
                {
                    ChangeState(PowerUpState.Flashing);
                }
                else if (timeRemaining <= despawnDuration) 
                {
                    ChangeState(PowerUpState.Despawning);
                }
                break;

            case PowerUpState.Flashing:
                if (spriteRenderer != null)
                {
                    float alpha = Mathf.PingPong(totalAliveTime * 20f, 1f) > 0.5f ? 1f : 0.2f;
                    Color c = spriteRenderer.color;
                    c.a = alpha;
                    spriteRenderer.color = c;
                }
                else if (spriteTransform != null)
                {
                    spriteTransform.gameObject.SetActive(Mathf.PingPong(totalAliveTime * 20f, 1f) > 0.5f);
                }

                if (timeRemaining <= despawnDuration)
                {
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
                float despawnT = Mathf.Clamp01(stateTimer / despawnDuration);
                ScaleTarget(Vector3.Lerp(initialScale, Vector3.zero, despawnT));
                if (despawnT >= 1f) Destroy(gameObject);
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
        if (spriteTransform != null) spriteTransform.localScale = newScale;
        else transform.localScale = newScale;
    }

    private float ElasticOut(float t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;
        float p = 0.3f;
        return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4) * (2 * Mathf.PI) / p) + 1;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (state == PowerUpState.Despawning && stateTimer > despawnDuration * 0.5f) return;

        CloudController cloud = collision.GetComponentInParent<CloudController>();
        if (cloud != null)
        {
            cloud.CollectPowerUp(type.ToString(), amount);
            if (collectEffect != null) Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(gameObject); 
        }
    }
}
