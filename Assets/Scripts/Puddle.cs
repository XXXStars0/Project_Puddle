using UnityEngine;

public class Puddle : MonoBehaviour
{
    [Header("Size")]
    public float startSize = 1f;
    public float maxSize = 3f; 
    public float currentSize;

    [Header("Environment")]
    public float evaporateRate = 0.2f; 
    public float sunEvaporationMultiplier = 1.0f; 

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] puddleVariants;

    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void Start()
    {
        if (spriteRenderer != null && puddleVariants != null && puddleVariants.Length > 0)
        {
            spriteRenderer.sprite = puddleVariants[Random.Range(0, puddleVariants.Length)];
        }
        currentSize = startSize;
        UpdateVisuals();
    }

    private void Update()
    {
        float decay = evaporateRate * sunEvaporationMultiplier * Time.deltaTime;
        ModifySize(-decay);
    }

    public void ModifySize(float amount)
    {
        currentSize += amount;

        if (currentSize <= 0f)
        {
            // Debug.Log("Dried up.");
            Destroy(gameObject);
            return;
        }

        currentSize = Mathf.Min(currentSize, maxSize);
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        transform.localScale = initialScale * currentSize;
    }

    public void SetSunIntensity(float multiplier)
    {
        sunEvaporationMultiplier = multiplier;
    }
}
